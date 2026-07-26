/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of EEBUSConformanceTests <https://github.com/OpenChargingCloud/EEBUSConformanceTests>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using Newtonsoft.Json.Linq;

using cloud.charging.open.protocols.EEBUS.SPINE;
using cloud.charging.open.protocols.EEBUS.SPINE.Model;
using cloud.charging.open.protocols.EEBUS.UseCases;
using cloud.charging.open.protocols.EEBUS.UseCases.CEVC;
using cloud.charging.open.protocols.EEBUS.UseCases.EVCC;
using cloud.charging.open.protocols.EEBUS.UseCases.EVCEM;
using cloud.charging.open.protocols.EEBUS.UseCases.EVCS;
using cloud.charging.open.protocols.EEBUS.UseCases.EVSECC;
using cloud.charging.open.protocols.EEBUS.UseCases.EVSOC;
using cloud.charging.open.protocols.EEBUS.UseCases.LPC;
using cloud.charging.open.protocols.EEBUS.UseCases.MPC;
using cloud.charging.open.protocols.EEBUS.UseCases.OPEV;
using cloud.charging.open.protocols.EEBUS.UseCases.OSCEV;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Simulations
{

    /// <summary>
    /// Replays a real device from its recorded answers.
    ///
    /// <c>libs/devices</c> holds what a set of shipped products actually said
    /// when somebody asked them: the detailed discovery and the use case data of
    /// wallboxes and heat pumps from Elli, Kostal, Porsche, SMA, Spelsberg,
    /// Vaillant, Viessmann and EVCC. Each file is a complete SHIP data message
    /// with a SPINE datagram inside it.
    ///
    /// This simulation feeds those datagrams into a real energy manager through
    /// the ordinary <see cref="SPINELocalDevice.ProcessDatagram"/> path - the
    /// same code which handles a live socket - and then reports what the manager
    /// made of them. Which is the point: it tests a client against **devices
    /// which exist**, including their quirks, with no hardware in the room.
    ///
    /// What comes out is the thing worth having: not "does our stack parse this"
    /// but "what does a real product announce, and would our energy manager
    /// actually be able to work with it". Older specification versions, entity
    /// hierarchies nobody would design, actors on the wrong use case - all of it
    /// arrives exactly as it did from the device.
    /// </summary>
    public class DeviceReplaySimulation : ASimulation
    {

        #region Data

        private SPINELocalDevice    hems     = null!;
        private SPINERemoteDevice   device   = null!;

        private readonly List<(String File, DatagramType Datagram)>  recordings = [];

        private readonly List<AUseCase>                              ourSide    = [];

        #endregion

        #region Properties

        /// <summary>What this simulation is called on the command line.</summary>
        public override String  Name         => "device-replay";

        /// <summary>What it shows.</summary>
        public override String  Description  => "replay a real device from libs/devices into an energy manager";

        /// <summary>
        /// Which device is being replayed, e.g. "porsche/mobile-charger-connect".
        /// </summary>
        public String           Device       { get; }

        /// <summary>
        /// Where the recordings live. Set before running to replay from
        /// somewhere other than the checked out submodule.
        /// </summary>
        public DirectoryInfo?   Recordings   { get; set; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create the replay simulation.
        /// </summary>
        /// <param name="Options">How it is to be run, including which device.</param>
        public DeviceReplaySimulation(SimulationOptions? Options = null)

            : base(Options)

        {

            Device = Options?.Device ?? "elli/charger-connect-pro";

        }

        #endregion


        #region (override) Build(CancellationToken)

        /// <summary>
        /// An energy manager, and the recorded answers of a device to feed it.
        /// </summary>
        protected override async Task Build(CancellationToken CancellationToken)
        {

            hems = new SPINELocalDevice("d:_i:19667_HEMS",
                                        DeviceTypeType.EnergyManagementSystem,
                                        TimeProvider: Clock.TimeProvider);

            var cem = hems.AddEntity(EntityTypeType.CEM);

            // Every client actor this stack has, so that the report can answer
            // the question worth asking: not "what did the device say" but
            // "could we work with it". Each of them watches the use case data as
            // it arrives and works out which scenarios it could play.
            ourSide.AddRange([
                new EVSECCEnergyManager   (cem),
                new EVCCEnergyManager     (cem),
                new EVCEMEnergyManager    (cem),
                new EVSOCMonitoringAppliance(cem),
                new OPEVEnergyGuard       (cem),
                new OSCEVEnergyManager    (cem),
                new EVCSEnergyBroker      (cem),
                new CEVCEnergyGuard       (cem),
                new CEVCEnergyBroker      (cem),
                new MPCMonitoringAppliance(cem),
                new LPCEnergyGuard        (cem)
            ]);

            foreach (var useCase in ourSide)
                await useCase.Register();

            // Nothing is ever sent back: a recording cannot answer. Whatever the
            // energy manager would say goes into the void, which is exactly what
            // a replay is.
            device = hems.AddRemoteDevice($"replay:{Device}", new Discard());

            var folder = Locate();

            if (folder is null)
            {
                Note("replay",
                     $"no recordings for '{Device}' - looked below {Root()?.FullName ?? "(no libs/devices checkout)"}");
                return;
            }

            foreach (var file in folder.GetFiles("*.json").OrderBy(file => file.Name, StringComparer.Ordinal))
            {

                var datagram = Read(file);

                if (datagram is not null)
                    recordings.Add((file.Name, datagram));

                else
                    Note("replay", $"{file.Name} holds no SPINE datagram");

            }

            Note("replay", $"{recordings.Count} recording(s) of '{Device}'");

        }

        #endregion

        #region (override) Script()

        /// <summary>
        /// Feed the recordings in, one every ten seconds, and then say what the
        /// energy manager learned.
        /// </summary>
        protected override IEnumerable<SimulationStep> Script()
        {

            var at = TimeSpan.Zero;

            foreach (var (file, datagram) in recordings)
            {

                at += TimeSpan.FromSeconds(10);

                var recording = datagram;
                var name      = file;

                yield return At(at, $"replay {name}", async cancellationToken => {

                    var refused = await hems.ProcessDatagram(recording, device, cancellationToken);

                    Note("replay",
                         refused is null
                             ? $"{name}: accepted"
                             : $"{name}: refused - {refused.Description}");

                });

            }

            yield return At(at + TimeSpan.FromSeconds(10), "report", cancellationToken => {

                Report();

                return Task.CompletedTask;

            });

        }

        #endregion

        #region (override) Settle

        /// <summary>Nothing has to settle: the report is the last step.</summary>
        protected override TimeSpan Settle => TimeSpan.Zero;

        #endregion


        #region (private) Report()

        /// <summary>
        /// What the energy manager now knows about the device, which is the
        /// answer this simulation exists to give.
        /// </summary>
        private void Report()
        {

            Note("energy manager", $"the device calls itself {device.DeviceAddress ?? "(no address)"}");

            foreach (var entity in device.Entities)
            {

                var features = entity.Features.
                                   Select(feature => $"{feature.FeatureType} ({feature.Role})").
                                   ToList();

                Note("energy manager",
                     $"entity [{String.Join(',', entity.EntityId)}] is a {entity.EntityType} with " +
                     $"{(features.Count > 0 ? String.Join(", ", features) : "no features")}");

            }

            var announced = new List<String>();

            foreach (var information in device.UseCases)
                foreach (var support in information.UseCaseSupport ?? [])
                {

                    announced.Add(support.UseCaseName ?? "");

                    Note("energy manager",
                         $"plays {support.UseCaseName} v{support.UseCaseVersion} as {information.Actor}, " +
                         $"scenarios {String.Join(", ", (support.ScenarioSupport ?? []).Order())}" +
                         $"{(support.UseCaseAvailable == false ? " (not available)" : "")}");

                }

            // And now the question this repository exists to answer: of what the
            // device announced, what could we actually do with it?
            var matched = new HashSet<String>(StringComparer.Ordinal);

            foreach (var useCase in ourSide)
            {

                var partner = device.Entities.
                                  Select(entity => useCase.PartnerFor(entity)).
                                  FirstOrDefault(found => found is not null);

                if (partner is null)
                    continue;

                matched.Add(useCase.Name);

                Note("our stack",
                     $"can play {useCase.Name} as {useCase.Actor} with entity " +
                     $"[{String.Join(',', partner.Entity.EntityId)}], scenarios " +
                     $"{String.Join(", ", partner.Scenarios.Order())}" +
                     $"{(partner.SameMajorVersion ? "" : " (a different major version)")}");

            }

            // Three different answers, and the middle one is the interesting
            // one: a use case we implement, which the device announces, and
            // which still did not match - almost always because the device put
            // it under an actor the specification does not give it. That is a
            // finding about the device, and it is the kind of thing which only
            // shows up against a real recording.
            foreach (var name in announced.Distinct().Order(StringComparer.Ordinal))
            {

                if (matched.Contains(name))
                    continue;

                var ours = ourSide.FirstOrDefault(useCase => useCase.Name == name);

                Note("our stack",
                     ours is null
                         ? $"does not implement the client side of {name}"
                         : $"implements {name} as {ours.Actor} but found no partner for it - " +
                           $"the device announced it under an actor this side does not accept " +
                           $"({String.Join(" or ", ours.PartnerActors)} expected)");

            }

        }

        #endregion

        #region (private) Root() / Locate() / Read(File)

        /// <summary>
        /// Where the recordings live: the submodule, unless told otherwise.
        ///
        /// Found by walking up from wherever this is running, because a
        /// simulation is started from a test runner as often as from the
        /// repository root.
        /// </summary>
        private DirectoryInfo? Root()
        {

            if (Recordings is not null)
                return Recordings.Exists ? Recordings : null;

            var here = new DirectoryInfo(AppContext.BaseDirectory);

            while (here is not null)
            {

                var devices = new DirectoryInfo(Path.Combine(here.FullName, "libs", "devices"));

                if (devices.Exists)
                    return devices;

                here = here.Parent;

            }

            return null;

        }


        /// <summary>
        /// The folder of the device being replayed, or null when there is none.
        /// </summary>
        private DirectoryInfo? Locate()
        {

            var root = Root();

            if (root is null)
                return null;

            var folder = new DirectoryInfo(Path.Combine(root.FullName,
                                                        Device.Replace('/', Path.DirectorySeparatorChar)));

            return folder.Exists ? folder : null;

        }


        /// <summary>
        /// The SPINE datagram inside a recorded file.
        ///
        /// The recordings are complete SHIP data messages, so the datagram sits
        /// two levels down - and some files are the datagram on its own, so both
        /// shapes are accepted.
        /// </summary>
        private static DatagramType? Read(FileInfo File)
        {

            try
            {

                var json = JObject.Parse(System.IO.File.ReadAllText(File.FullName));

                var body = json["data"]?["payload"]?["datagram"]
                               ?? json["payload"]?["datagram"]
                               ?? json["datagram"];

                return body is not null
                           ? SPINEJSON.Read<DatagramType>(body)
                           : null;

            }
            catch
            {
                return null;
            }

        }

        #endregion

        #region (private class) Discard

        /// <summary>
        /// Where datagrams go when the other side is a recording.
        /// </summary>
        private sealed class Discard : ISPINEWriter
        {

            public Task SendSPINEDatagram(JObject            Datagram,
                                          CancellationToken  CancellationToken   = default)

                => Task.CompletedTask;

        }

        #endregion

    }

}

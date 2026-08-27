using MaterialControlSimulator.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MaterialControlSimulator
{
    public class CarrierStateManager
    {
        private readonly string _filePath =
            Path.Combine(
                AppContext.BaseDirectory,
                "Data",
                "CarrierState.json");

        public void Save()
        {
            var states =
                App.CarrierManager.Sessions
                    .Select(session =>
                        new CarrierState
                        {
                            CarrierId =
                                session.Carrier.Id,

                            CurrentNodeId =
                                session.Carrier.CurrentNode?.Id ?? "",

                            DestinationNodeId =
                                session.Destination?.Id ?? "",

                            MaxCellCount =
                                session.Carrier.MaxCellCount,

                            CurrentCellCount =
                                session.Carrier.CurrentCellCount,
                        })
                    .ToList();

            string? directory =
                Path.GetDirectoryName(_filePath);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var options =
                new JsonSerializerOptions
                {
                    WriteIndented = true
                };

            string json =
                JsonSerializer.Serialize(
                    states,
                    options);

            File.WriteAllText(
                _filePath,
                json);
        }

        public List<CarrierState> Load()
        {
            if (!File.Exists(_filePath))
                return new List<CarrierState>();

            string json =
                File.ReadAllText(_filePath);

            return JsonSerializer.Deserialize
                       <List<CarrierState>>(json)
                   ?? new List<CarrierState>();
        }

        public async Task RestoreCarriers(SimulationManager _simulation, Canvas canvas)
        {
            var states =
                App.CarrierStateManager.Load();

            foreach (var state in states)
            {
                var currentNode =
                    App.Nodes.Get(
                        state.CurrentNodeId);

                if (currentNode == null)
                    continue;


                CarrierControl carrier = new CarrierControl { Id = state.CarrierId, CurrentNode = currentNode };
                // 기존 Carrier 생성 메서드 사용

                canvas.Children.Add(carrier);

                App.CarrierManager.Register(carrier);

                currentNode.TryEnter(carrier);

                if (carrier.CurrentNode is CarrierProcessControl)
                {
                   await (carrier.CurrentNode as CarrierProcessControl).ProcessAsync(carrier);
                }

                carrier.MaxCellCount =
                    state.MaxCellCount;

                carrier.CurrentCellCount =
                    state.CurrentCellCount;

                // 애니메이션 없이 바로 현재 위치에 배치
                carrier.SetPosition(
                    currentNode);

                // Session 생성/조회
                var session =
                    App.CarrierManager.Get(
                        carrier.Id);

                if (session == null)
                    continue;

      
                // 목적지 복원
                if (!string.IsNullOrWhiteSpace(state.DestinationNodeId) && state.CurrentNodeId != state.DestinationNodeId)
                {
                    var destination =
                        App.Nodes.Get(
                            state.DestinationNodeId);

                    if (destination != null)
                    {
                        // 기존 라우팅 등록 메서드 사용
                        _simulation.SetDestination(
                            session,
                            destination);
                    }
                }
            }
        }
    }
}

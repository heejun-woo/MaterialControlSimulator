using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MaterialControlSimulator.Controls;
using System.Diagnostics;

namespace MaterialControlSimulator
{
    public class CarrierManager
    {
        public async Task ExecuteAsync(
            MoveCommand command)
        {
            var carrier =
                App.Carriers.Get(command.CarrierId);


            var targetNode =
                App.Nodes.Get(command.DestinationId);


            // 목적지가 이미 사용 중인지 확인
            if (!targetNode.Enter(carrier))
            {
                Debug.WriteLine(
                    $"Node 사용중 : {targetNode.Id}");

                return;
            }


            // 기존 위치 저장
            var oldNode = carrier.CurrentNode;


            // 이동
            await carrier.MoveToAsync(
                targetNode.GetPosition(),
                command.Speed);


            // 기존 위치 해제
            oldNode?.Leave();


            // 현재 위치 갱신
            carrier.CurrentNode = targetNode;
        }
    }
}

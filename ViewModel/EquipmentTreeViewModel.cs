using MaterialControlSimulator.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator
{
    public class EquipmentTreeViewModel
    {
        public ObservableCollection<NodeTreeItem> Nodes { get; } = new();


        public EquipmentTreeViewModel()
        {
            App.Nodes.NodeList.CollectionChanged += Nodes_CollectionChanged;
        }

        private void Nodes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null)
                return;

            foreach (NodeControl node in e.NewItems)
            {
                Nodes.Add(
                    new NodeTreeItem
                    {
                        Name = node.Id,
                        Type = node.GetType().Name,
                        Control = node
                    });
            }
        }
    }
}

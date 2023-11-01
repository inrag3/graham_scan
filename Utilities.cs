using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace graham
{
    public class Utilities
    {
        public static void Find<T>(DependencyObject root, List<T> container)
        {
            Stack<DependencyObject> objects = new Stack<DependencyObject>();

            objects.Push(root);

            while (objects.Count > 0)
            {
                var x = objects.Pop();
                if (x is T element)
                {
                    container.Add(element);
                }
                int count = VisualTreeHelper.GetChildrenCount(x);
                for (int i = 0; i < count; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(x, i);
                    objects.Push(child);
                }
            }
        }
    }
}

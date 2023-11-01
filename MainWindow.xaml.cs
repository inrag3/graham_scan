using Drawer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace graham
{

    public partial class MainWindow : Window
    {

        private const int THRESHOLD = 25;

        private enum Action
        {
            NONE,
            DRAW,
            DELETE,
        }

        Drawer.Drawer _drawer;
        private readonly List<Drawer.Point> _points = new();
        Action _action = Action.NONE;

        private readonly List<Button> _buttons = new();

        bool _isEditing = false;

        public MainWindow()
        {
            InitializeComponent();
            _drawer = new Drawer.Drawer(800, 650);
            UpdateImage();
            ContentRendered += OnContentRendered;
        }

        private void OnContentRendered(object? sender, EventArgs e)
        {
            Utilities.Find(this, _buttons);
        }

        void DisableInterface()
        {
            _buttons.ForEach(button => button.IsEnabled = false);
        }

        void EnableInterface()
        {
            _buttons.ForEach(button =>
            {
                button.IsEnabled = true;
                button.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(112, 112, 112));
            });
        }

        void HightlightButton(Button btn)
        {
            btn.IsEnabled = true;
            btn.BorderBrush = System.Windows.Media.Brushes.Red;
        }

        void StartImageEditing(Action action)
        {
            _isEditing = true;
            _action = action;
        }

        void StopImageEditing()
        {
            _isEditing = false;
            _action = Action.NONE;
        }

        void UpdateImage()
        {
            Image.Source = _drawer.Bitmap;
        }

        private void DefinePointButton_Click(object sender, RoutedEventArgs e)
        {
            DisableInterface();
            HightlightButton(DefinePointButton);
            StartImageEditing(Action.DRAW);
        }

        private void DeletePointButton_Click(object sender, RoutedEventArgs e)
        {
            if (_points.Count() == 0)
                return;
            DisableInterface();
            HightlightButton(DeletePointButton);
            StartImageEditing(Action.DELETE);
        }

        private void CreateHullButton_Click(object sender, RoutedEventArgs e)
        {
            if (_points.Count() == 0)
                return;
            CreateHull();
        }

        private void CreateHull()
        {
            var hull = new Hull(_points);
           
            _drawer.DrawPolygon(hull.Points, Drawer.Colors.Red);
        }

        private void DrawPoint(int x, int y)
        {
            var point = new Drawer.Point(x, y);
            _drawer.DrawPoint(point, Drawer.Colors.Blue, 6);
            _points.Add(point);
            EnableInterface();
            StopImageEditing();
        }


        private Drawer.Point? DetectPoint(int x, int y)
        {
            var point = new Drawer.Point(x, y);
            var target = _points.OrderBy(p => p.Distance(point)).First();
            if (target.Distance(point) > THRESHOLD)
                return null;
            return target;
        }

        private void DeletePoint(int x, int y)
        {
            var target = DetectPoint(x, y);
            if (target == null)
                return;
            _points.Remove(target);
            Redraw();
            EnableInterface();
            StopImageEditing();
        }

        private void Redraw()
        {
            _drawer.Clear();
            _points.ForEach(x => _drawer.DrawPoint(x, Drawer.Colors.Blue, 6));
            UpdateImage();
        }


        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isEditing)
                return;

            var position = e.GetPosition(Image);
            var x = (int)position.X;
            var y = (int)position.Y;

            switch (_action)
            {
                case Action.DRAW:
                    DrawPoint(x, y);
                    break;
                case Action.DELETE:
                    DeletePoint(x, y);
                    break;
            }
            UpdateImage();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _points.Clear();
            _drawer.Clear();
            UpdateImage();
        }
    }
}

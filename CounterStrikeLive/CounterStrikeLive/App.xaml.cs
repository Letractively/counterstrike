﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Diagnostics;

namespace CounterStrikeLive
{
    public partial class App : Application
    {

        public App()
        {            
            this.Startup += this.Application_Startup;
            this.Exit += this.Application_Exit;
            //this.UnhandledException += this.Application_UnhandledException;

            InitializeComponent();
        }
        Menu menu;
        Menu _Menu{get{return menu;} set{menu = value;}}
        private void Application_Startup(object sender, StartupEventArgs e)
        {
                        
            this.RootVisual =_Menu= new Menu();
        }

        private void Application_Exit(object sender, EventArgs e)
        {

        }
        ApplicationUnhandledExceptionEventArgs e;
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            this.e = e;
            _Menu.Dispatcher.BeginInvoke(delegate()
            {
                Trace.WriteLine(e.ExceptionObject.ToString());
                _Menu.Dispatcher.BeginInvoke(_Menu._Console.Show);                
            });
            e.Handled = true;
        }
        
    }
}
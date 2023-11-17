﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessengerApp.Stores;
using MessengerApp.ViewModels;
using MessengerDashboard.UI;
using System.Diagnostics;
using MessengerDashboard.Client;
using MessengerWhiteboard;

namespace MessengerApp.Commands
{
    internal class NavigateServerWhiteboardCommand : CommandBase
    {
        private readonly NavigationStore _navigationStore;

        private readonly ViewModel _whiteboardViewModel;

        //private readonly string 

        public NavigateServerWhiteboardCommand(NavigationStore navigationStore, ViewModel whiteboardViewModel)
        {
            _navigationStore = navigationStore;
            _whiteboardViewModel = whiteboardViewModel;
        }

        public override void Execute(object parameter)
        {
            _navigationStore.SubViewModel = _whiteboardViewModel;
        }
    }
}

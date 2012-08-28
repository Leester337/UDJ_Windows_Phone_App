/*
  Copyright 2011 Kurtis L. Nusbaum

  This file is part of UDJ.

  UDJ is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 2 of the License, or
  (at your option) any later version.

  UDJ is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with UDJ.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace UDJ
{
    public class LibraryEntry
    {
        public string id { get; set; }
        public string title { get; set; }
        public string artist { get; set; }
        public string album { get; set; }
        public int track { get; set; }
        public string genre { get; set; }
        public int duration { get; set; }
        public Visibility isVisible
        {
            get
            {
                return album != "" ? Visibility.Visible : Visibility.Collapsed;
            }
            set
            {
                isVisible = value;
            }
        }

    }
}

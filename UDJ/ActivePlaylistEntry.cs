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
using System.Collections.Generic;

namespace UDJ
{
    public class ActivePlaylistEntry
    {
        public LibraryEntry song { get; set; }
        public List<User> upvoters { get; set; }
        public List<User> downvoters { get; set; }
        public string time_added { get; set; }
        public User adder { get; set; }
        public long total_votes { get; set; }
        public string title { get; set; }
        public string artist { get; set; }



        public void updatePageInfo()
        {
            total_votes = upvoters.Count - downvoters.Count >= 0 ? upvoters.Count - downvoters.Count: 0;
            title = song.title;
            artist = song.artist;
        }

    }
}

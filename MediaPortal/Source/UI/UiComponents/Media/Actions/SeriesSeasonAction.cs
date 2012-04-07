#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Actions
{
  public class SeriesSeasonAction : TrackServerConnectionBaseAction
  {
    #region Consts

    public const string VIDEOS_CONTRIBUTOR_MODEL_ID_STR = "ECD9729E-AE51-448E-85C2-A8302E9BB392";

    public static readonly Guid VIDEOS_CONTRIBUTOR_MODEL_ID = new Guid(VIDEOS_CONTRIBUTOR_MODEL_ID_STR);

    public const string RES_SERIES_SEASON_MENU_ITEM = "[Media.SeriesSeasonMenuItem]";

    #endregion

    public SeriesSeasonAction() :
        base(true, Consts.WF_STATE_ID_SERIES_NAVIGATION_ROOT, RES_SERIES_SEASON_MENU_ITEM) { }
  }
}
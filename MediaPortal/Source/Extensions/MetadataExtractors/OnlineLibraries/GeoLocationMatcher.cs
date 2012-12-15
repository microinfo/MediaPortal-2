﻿#region Copyright (C) 2007-2012 Team MediaPortal

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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.OnlineLibraries.Libraries.GeoLocation;
using MediaPortal.Extensions.OnlineLibraries.Libraries.GeoLocation.Data;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// <see cref="GeoLocationMatcher"/> is used to lookup geographic locations from given coordinates (latitude, longitude).
  /// </summary>
  public class GeoLocationMatcher
  {
    #region Static instance

    public static GeoLocationMatcher Instance
    {
      get { return ServiceRegistration.Get<GeoLocationMatcher>(); }
    }

    #endregion

    public bool TryLookup(double latitude, double longitude, out LocationInfo locationInfo)
    {
      try
      {
        IGeolocationLookup lookup = new OsmNominatim();
        return lookup.TryLookup(latitude, longitude, out locationInfo);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error while executing reverse geocoding.", ex);
        locationInfo = null;
        return false;
      }
    }

    private static double DegreesToRadians(double degrees)
    {
      return degrees * Math.PI / 180.0;
    }

    public static double CalculateDistance(LocationInfo location1, LocationInfo location2)
    {
      const double EARTH_RADIUS_KM = 6376.5;
      double lat1InRad = DegreesToRadians(location1.Latitude);
      double long1InRad = DegreesToRadians(location1.Longitude);
      double lat2InRad = DegreesToRadians(location2.Latitude);
      double long2InRad = DegreesToRadians(location2.Longitude);

      double dLongitude = long2InRad - long1InRad;
      double dLatitude = lat2InRad - lat1InRad;
      double a = Math.Pow(Math.Sin(dLatitude / 2), 2) + Math.Cos(lat1InRad) * Math.Cos(lat2InRad) * Math.Pow(Math.Sin(dLongitude / 2), 2);
      double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
      return EARTH_RADIUS_KM * c;
    }

    public static double CalculateDistance(params LocationInfo[] locations)
    {
      double totalDistance = 0.0;

      for (int i = 0; i < locations.Length - 1; i++)
      {
        LocationInfo current = locations[i];
        LocationInfo next = locations[i + 1];

        totalDistance += CalculateDistance(current, next);
      }

      return totalDistance;
    }
  }
}
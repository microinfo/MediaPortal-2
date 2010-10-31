#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Collections.Generic;

namespace MediaPortal.Core.MediaManagement.ResourceAccess
{
  /// <summary>
  /// Interface of the MediaPortal 2 resource information service. This interface is implemented by both
  /// the MediaPortal 2 client and server to provide information about resources.
  /// </summary>
  public interface IResourceInformationService
  {
    ICollection<string> GetMediaCategoriesFromMetadataExtractors();
    ICollection<MediaProviderMetadata> GetAllBaseMediaProviderMetadata();
    MediaProviderMetadata GetMediaProviderMetadata(Guid mediaProviderId);
    string GetResourcePathDisplayName(ResourcePath path);
    string GetResourceDisplayName(ResourcePath path);
    ICollection<ResourcePathMetadata> GetChildDirectoriesData(ResourcePath path);
    ICollection<ResourcePathMetadata> GetFilesData(ResourcePath path);
    bool DoesResourceExist(ResourcePath path);
    bool GetResourceInformation(ResourcePath path, out bool isFileSystemResource,
        out bool isFile, out string resourcePathName, out string resourceName, out DateTime lastChanged, out long size);
    bool DoesMediaProviderSupportTreeListing(Guid mediaProviderId);

    ResourcePath ExpandResourcePathFromString(Guid mediaProviderId, string path);
    ResourcePath ConcatenatePaths(ResourcePath basePath, string relativePath);
    string GetResourceServerBaseURL();
  }
}
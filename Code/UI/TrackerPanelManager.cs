﻿// <copyright file="TrackerPanelManager.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace FlightTracker
{
    using AlgernonCommons;
    using AlgernonCommons.UI;
    using ColossalFramework;

    /// <summary>
    /// Static class to manage the flight tracker panel.
    /// </summary>
    internal static class TrackerPanelManager
    {
        /// <summary>
        /// Creates the panel object in-game and displays it.
        /// </summary>
        internal static void Create() => StandalonePanelManager<TrackerPanel>.Create();

        /// <summary>
        /// Closes the panel by destroying the object (removing any ongoing UI overhead).
        /// </summary>
        internal static void Close() => StandalonePanelManager<TrackerPanel>.Panel?.Close();

        /// <summary>
        /// Sets the target to the selected building, creating the panel if necessary.
        /// </summary>
        /// <param name="buildingID">New building ID.</param>
        internal static void SetTarget(ushort buildingID)
        {
            // If no existing panel, create it.
            if (!StandalonePanelManager<TrackerPanel>.Panel)
            {
                Create();
            }

            // Set the target.
            StandalonePanelManager<TrackerPanel>.Panel.SetTarget(buildingID);
        }

        /// <summary>
        /// Handles target building changes.
        /// </summary>
        internal static void TargetChanged()
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;

            // Set target to this building if it's supported, or close if it's an unsupported building.
            if (IsSupportedBuilding(buildingID))
            {
                SetTarget(buildingID);
            }
            else
            {
                Close();
            }
        }

        /// <summary>
        /// Checks to see if the given building is supported by the mod.
        /// </summary>
        /// <param name="buildingID">Building ID of building to check.</param>
        /// <returns>A value indicating whether the given building is supported.</returns>
        private static bool IsSupportedBuilding(ushort buildingID)
        {
            if (buildingID != 0)
            {
                Logging.Message("checking building ", buildingID);

                BuildingInfo buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;
                if (buildingInfo != null && buildingInfo.GetSubService() == ItemClass.SubService.PublicTransportPlane)
                {
                    Logging.Message("building supported");
                    return true;
                }
            }

            // If we got here, no supported building was found.
            return false;
        }
    }
}
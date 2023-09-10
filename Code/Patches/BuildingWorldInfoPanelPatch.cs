// <copyright file="BuildingWorldInfoPanelPatch.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace FlightTracker
{
    using System.Collections.Generic;
    using System.Reflection;
    using HarmonyLib;

    /// <summary>
    /// Harmony patch to handle building selection changes when the info panel is open.
    /// </summary>
    [HarmonyPatch(typeof(BuildingWorldInfoPanel))]
    public static class BuildingWorldInfoPanelPatch
    {
        /// <summary>
        /// Harmony Postfix patch to update tracker panel target (and/or visibility) when building selection changes.
        /// </summary>
        [HarmonyPatch("OnSetTarget")]
        [HarmonyPostfix]
        public static void Postfix()
        {
            TrackerPanelManager.TargetChanged();
        }
    }
}
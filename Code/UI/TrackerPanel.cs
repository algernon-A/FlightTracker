// <copyright file="TrackerPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace FlightTracker
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// The flight tracker panel.
    /// </summary>
    internal class TrackerPanel : StandalonePanel
    {
        // Layout constants - private.
        private const float ListY = 40f;
        private const float ListHeight = 10f * FlightRow.FlightRowHeight;
        private const float ListWidth = 400f;
        private const float CalculatedPanelHeight = ListY + ListHeight + Margin;
        private const float CalculatedPanelWidth = 400f + Margin + Margin;

        // List of flights.
        private readonly List<FlightRowData> _tempList = new List<FlightRowData>();

        // Panel components.
        private UIList _flightList;

        // Selected target.
        private ushort _buildingID;

        // Flag to indicate that position needs to be adjusted.
        private bool _adjustPos = false;

        /// <summary>
        /// Gets the panel width.
        /// </summary>
        public override float PanelWidth => CalculatedPanelWidth;

        /// <summary>
        /// Gets the panel height.
        /// </summary>
        public override float PanelHeight => CalculatedPanelHeight;

        /// <summary>
        /// Gets the panel's title.
        /// </summary>
        protected override string PanelTitle => Translations.Translate("MOD_NAME");

        /// <summary>
        /// Called by Unity when the object is created.
        /// Used to perform setup.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            try
            {
                // Flight list.
                _flightList = UIList.AddUIList<FlightRow>(
                    this,
                    Margin,
                    ListY,
                    ListWidth,
                    ListHeight,
                    FlightRow.FlightRowHeight);
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up flight tracker panel");
            }
        }

        /// <summary>
        /// Called by Unity every update.
        /// </summary>
        public override void Update()
        {
            base.Update();

            // Adjust position if we need to, to be to the left of the info panel.
            if (_adjustPos)
            {
                if (UIView.library.Get<CityServiceWorldInfoPanel>(typeof(CityServiceWorldInfoPanel).Name)?.component is UIComponent infoPanel && infoPanel.isVisible && infoPanel.isActiveAndEnabled)
                {
                    relativePosition = infoPanel.relativePosition - new Vector3(PanelWidth + Margin, -40f);
                    _adjustPos = false;
                    isVisible = true;
                }
            }

            // Local references.
            Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
            Vehicle[] vehicleBuffer = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;
            NetNode[] nodeBuffer = Singleton<NetManager>.instance.m_nodes.m_buffer;

            // Regenerate vehicle list.
            _tempList.Clear();
            ushort vehicleID = buildingBuffer[_buildingID].m_ownVehicles;
            while (vehicleID != 0)
            {
                // Local reference.
                ref Vehicle thisVehicle = ref vehicleBuffer[vehicleID];

                // Only interested in passenger aircraft.
                VehicleInfo vehicleInfo = thisVehicle.Info;
                if (vehicleInfo == null || vehicleInfo.m_class.m_subService != ItemClass.SubService.PublicTransportPlane)
                {
                    // Make sure that the next vehicle ID is assigned before continuing, otherwise there'll be an infinite loop.
                    vehicleID = thisVehicle.m_nextOwnVehicle;
                    continue;
                }

                // Determine flight status for this vehicle.
                FlightRowData.FlightStatus flightStatus = FlightRowData.FlightStatus.Incoming;
                ushort vehicleTarget = thisVehicle.m_targetBuilding;
                if (vehicleTarget != 0)
                {
                    // If vehicle target node is near map edge, then it's departing.
                    Vector3 nodePos = nodeBuffer[vehicleTarget].m_position;
                    if (nodePos.x < -8500 || nodePos.x > 8500 || nodePos.z < -8500 || nodePos.z > 8500)
                    {
                        // Check to see if it's still at the gate.
                        if ((vehicleBuffer[vehicleID].m_flags & Vehicle.Flags.Stopped) != 0)
                        {
                            // At gate.
                            flightStatus = FlightRowData.FlightStatus.AtGate;
                        }
                        else
                        {
                            // It's left the gate.
                            flightStatus = FlightRowData.FlightStatus.Departed;
                        }
                    }
                }

                // Check for 'landed' status for arriving flights.
                if (flightStatus == FlightRowData.FlightStatus.Incoming && (thisVehicle.m_flags & Vehicle.Flags.Flying) == 0)
                {
                    // Exclude vehicles landed near map edge from being recorded as 'landed'.
                    Vector3 vehiclePos = thisVehicle.GetLastFramePosition();
                    if (vehiclePos.x > -8500 && vehiclePos.x < 8500 && vehiclePos.z > -8500 && vehiclePos.z < 8500)
                    {
                        flightStatus = FlightRowData.FlightStatus.Landed;
                    }
                }

                // Add this row to the list.
                _tempList.Add(new FlightRowData(vehicleID, vehicleInfo, flightStatus));

                // Next vehicle.
                vehicleID = thisVehicle.m_nextOwnVehicle;
            }

            // Set display list items, without changing the display.
            _flightList.Data = new FastList<object>
            {
                m_buffer = _tempList.ToArray(),
                m_size = _tempList.Count,
            };
        }

        /// <summary>
        /// Applies the panel's default position.
        /// </summary>
        public override void ApplyDefaultPosition()
        {
            // Set the flag to check/adjust the position at the next update.
            isVisible = false;
            _adjustPos = true;
        }

        /// <summary>
        /// Sets/changes the currently selected building.
        /// </summary>
        /// <param name="buildingID">New building ID.</param>
        internal virtual void SetTarget(ushort buildingID)
        {
            Logging.Message("target set to building ", buildingID);
            _buildingID = buildingID;

            // Adjust panel position to match the new building.
            _adjustPos = true;
        }
    }
}

// <copyright file="TrackerPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace FlightTracker
{
    using System;
    using System.Collections.Generic;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// The flight tracker panel.
    /// </summary>
    internal class TrackerPanel : UIPanel
    {
        // Layout constants - private.
        private const float Margin = 5f;
        private const float TitleHeight = 40f;
        private const float NameLabelY = TitleHeight + Margin;
        private const float NameLabelHeight = 30f;
        private const float ListY = TitleHeight + NameLabelHeight;
        private const float ListHeight = 10f * FlightRow.FlightRowHeight;
        private const float ListWidth = 400f;
        private const float PanelHeight = ListY + ListHeight + Margin;
        private const float PanelWidth = 400f + Margin + Margin;

        // Panel components.
        private UILabel _buildingLabel;
        private UIList _flightList;

        // List of flights.
        private List<FlightRowData> _tempList = new List<FlightRowData>();

        // Selected target.
        private ushort _buildingID;

        /// <summary>
        /// Called by Unity when the object is created.
        /// Used to perform setup.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            try
            {
                // Basic setup.
                autoLayout = false;
                backgroundSprite = "UnlockingPanel2";
                isVisible = true;
                canFocus = true;
                isInteractive = true;
                width = PanelWidth;
                height = PanelHeight;

                // Default position - centre in screen.
                relativePosition = new Vector2(Mathf.Floor((GetUIView().fixedWidth - PanelWidth) / 2), (GetUIView().fixedHeight - PanelHeight) / 2);

                // Title label.
                UILabel titleLabel = UILabels.AddLabel(this, 0f, 10f, Translations.Translate("MOD_NAME"), PanelWidth, 1.2f);
                titleLabel.textAlignment = UIHorizontalAlignment.Center;

                // Building label.
                _buildingLabel = UILabels.AddLabel(this, 0f, NameLabelY, string.Empty, PanelWidth);
                _buildingLabel.textAlignment = UIHorizontalAlignment.Center;

                // Drag handle.
                UIDragHandle dragHandle = this.AddUIComponent<UIDragHandle>();
                dragHandle.relativePosition = Vector3.zero;
                dragHandle.width = PanelWidth - 35f;
                dragHandle.height = TitleHeight;

                // Close button.
                UIButton closeButton = AddUIComponent<UIButton>();
                closeButton.relativePosition = new Vector2(width - 35f, 2f);
                closeButton.normalBgSprite = "buttonclose";
                closeButton.hoveredBgSprite = "buttonclosehover";
                closeButton.pressedBgSprite = "buttonclosepressed";

                // Flight list.
                _flightList = UIList.AddUIList<FlightRow>(
                    this,
                    Margin,
                    ListY,
                    ListWidth,
                    ListHeight,
                    FlightRow.FlightRowHeight);

                // Close button event handler.
                closeButton.eventClick += (component, clickEvent) =>
                {
                    TrackerPanelManager.Close();
                };
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

            // Local references.
            Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
            Vehicle[] vehicleBuffer = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;

            // Regenerate vehicle list.
            _tempList.Clear();
            ushort vehicleID = buildingBuffer[_buildingID].m_ownVehicles;
            while (vehicleID != 0)
            {
                // Only interested in passenger aircraft.
                VehicleInfo vehicleInfo = vehicleBuffer[vehicleID].Info;
                if (vehicleInfo == null || vehicleInfo.m_class.m_subService != ItemClass.SubService.PublicTransportPlane)
                {
                    // Make sure that the next vehicle ID is assigned before continuing, otherwise there'll be an infinite loop.
                    vehicleID = vehicleBuffer[vehicleID].m_nextOwnVehicle;
                    continue;
                }

                // Determine flight status for this vehicle.
                FlightRowData.FlightStatus flightStatus = FlightRowData.FlightStatus.Arriving;
                ushort vehicleTarget = vehicleBuffer[vehicleID].m_targetBuilding;
                if (vehicleTarget != 0)
                {
                    // If vehicle target node is near map edge, then it's departing.
                    Vector3 nodePos = Singleton<NetManager>.instance.m_nodes.m_buffer[vehicleTarget].m_position;
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
                if (flightStatus == FlightRowData.FlightStatus.Arriving && (vehicleBuffer[vehicleID].m_flags & Vehicle.Flags.Flying) == 0)
                {
                    // Exclude vehicles landed near map edge from being recorded as 'landed'.
                    Vector3 vehiclePos = vehicleBuffer[vehicleID].GetLastFramePosition();
                    if (!(vehiclePos.x < -8500 || vehiclePos.x > 8500 || vehiclePos.z < -8500 || vehiclePos.z > 8500))
                    {
                        flightStatus = FlightRowData.FlightStatus.Landed;
                    }
                }

                // Add this row to the list.
                _tempList.Add(new FlightRowData(vehicleID, vehicleInfo, flightStatus));

                // Next vehicle.
                vehicleID = vehicleBuffer[vehicleID].m_nextOwnVehicle;
            }

            // Set display list items, without changing the display.
            _flightList.Data = new FastList<object>
            {
                m_buffer = _tempList.ToArray(),
                m_size = _tempList.Count,
            };
        }

        /// <summary>
        /// Sets/changes the currently selected building.
        /// </summary>
        /// <param name="buildingID">New building ID.</param>
        internal virtual void SetTarget(ushort buildingID)
        {
            Logging.KeyMessage("target set to building ", buildingID);
            _buildingID = buildingID;
        }
    }
}

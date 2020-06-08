﻿/**
 * Copyright 2020 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Google.Maps.Demos.Zoinkies {

    public class EnergyStationController : BaseSpawnLocationController {

        protected override void ActionState() {

            if (IsLoading)
                return;

            base.ActionState();

            // Testing - pick first energy station from world
            List<SpawnLocation> s =
                new List<SpawnLocation>(WorldService.GetInstance().GetEnergyStations());
            LocationId = s.ElementAt(0).id;

            if (String.IsNullOrEmpty(LocationId)) {
                Debug.LogError("Incorrect PlaceId!");
                UIManager.OnShowLoadingView(false);
                return;
            }

            // Check if this station is active?
            // If not show a floating popup with timeout information
            if (WorldService.GetInstance().IsRespawning(LocationId)) {

                DateTime t = DateTime.Parse(location.respawn_time);
                TimeSpan timeLeft = t.Subtract(DateTime.Now);
                UIManager.OnShowMessageDialog("Station recharging. Time left: ", timeLeft);
                UIManager.OnShowLoadingView(false);
                return;
            }

            try {
                IsLoading = true;
                StartCoroutine(ServerManager.PostRechargingStation(LocationId, OnSuccess, OnError));
            }
            catch (System.Exception e) {
                Debug.LogError("Failed to open chest! " + e.ToString());
                IsLoading = false;
                UIManager.OnShowLoadingView(false);
            }
        }

        private void OnSuccess(EnergyData data) {
            IsLoading = false;
            if (data == null) {
                // The station is recharging
                return;
            }

            WorldService.GetInstance().StartRespawn(LocationId);

            // Init the player's data
            PlayerService.GetInstance().IncreaseEnergyLevel(data.amountRestored);

            UIManager.OnShowLoadingView(false);

            UIManager.OnShowMessageDialog("Energy recharged to 100%!");
        }

        protected override SpawnLocation GetTestingLocation() {
            // Pick first available minion
            return WorldService.GetInstance().GetEnergyStations().ElementAt(0);
        }
    }
}

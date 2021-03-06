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

using System.Collections.Generic;
using Google.Maps.Feature.Style;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace Google.Maps.Demos.Zoinkies
{
    /// <summary>
    ///     This class initializes reference, player, map data.
    ///     It also manages the creation and maintenance of spawned game objects.
    /// </summary>
    public class WorldController : BaseMapLoader
    {
        /// <summary>
        ///     Reference to avatar
        /// </summary>
        public GameObject Avatar;

        /// <summary>
        ///     Dispatched to the game when Reference Data, World Data and Player Data
        ///     are initialized.
        /// </summary>
        public UnityEvent GameReady;

        /// <summary>
        ///     Reference to ground material
        /// </summary>
        public Material GroundMaterial;

        /// <summary>
        ///     Reference to the main camera
        /// </summary>
        public Camera MainCamera;

        /// <summary>
        ///     The vertical scaling factor applied at maximum squashing.
        /// </summary>
        public float MaximumSquash = 0.1f;

        /// <summary>
        ///     Reference to roads material
        /// </summary>
        public Material RoadsMaterial;

        /// <summary>
        ///     Reference to list of building walls materials
        /// </summary>
        public List<Material> BuildingsWallMaterials;

        /// <summary>
        ///     Reference to list of building roof materials
        /// </summary>
        public List<Material> BuildingsRoofMaterials;

        /// <summary>
        ///     Reference to material for modeled structures
        /// </summary>
        public Material ModeledBuildingsMaterial;

        /// <summary>
        ///     Reference to the server Manager, responsible for all REST calls.
        /// </summary>
        public ServerManager ServerManager;

        /// <summary>
        ///     Distance outside which buildings will not be squashed.
        /// </summary>
        public float SquashFar = 200;

        /// <summary>
        ///     Distance inside which buildings will be completely squashed (<see cref="MaximumSquash" />)
        /// </summary>
        public float SquashNear = 50;

        /// <summary>
        ///     Setup milestone: Reference data loaded and initialized.
        /// </summary>
        private const string REFERENCE_DATA_INITIALIZED = "REFERENCE_DATA_INITIALIZED";

        /// <summary>
        ///     Setup milestone: Player data loaded and initialized.
        /// </summary>
        private const string PLAYER_DATA_INITIALIZED = "PLAYER_DATA_INITIALIZED";

        /// <summary>
        ///     Setup milestone: Map data loaded and initialized.
        /// </summary>
        private const string MAP_INITIALIZED = "MAP_INITIALIZED";

        /// <summary>
        ///     Indicates if the floating origin has been set
        /// </summary>
        private bool _floatingOriginIsSet;

        /// <summary>
        ///     Indicates if the game has started
        /// </summary>
        private bool _gameStarted;

        /// <summary>
        ///     Keeps track of startup milestones
        /// </summary>
        private List<string> _startupCheckList;

        /// <summary>
        ///     Reference to the styles options applied to loaded map features
        /// </summary>
        private GameObjectOptions _zoinkiesStylesOptions;

        /// <summary>
        ///     Sets up the setup milestones list.
        ///     Loads reference data and player data.
        /// </summary>
        void Awake()
        {
            _gameStarted = false;

            // Initialize start up check list
            _startupCheckList = new List<string>
            {
                REFERENCE_DATA_INITIALIZED,
                PLAYER_DATA_INITIALIZED,
                MAP_INITIALIZED
            };

            // Load and initialize Reference Data
            ReferenceService.GetInstance().Init(ServerManager.GetReferenceData());
            _startupCheckList.Remove(REFERENCE_DATA_INITIALIZED);

            // Load and initialize Player Data
            PlayerService.GetInstance().Init(ServerManager.GetPlayerData());
            _startupCheckList.Remove(PLAYER_DATA_INITIALIZED);

            LoadOnStart = false;

            //CheckStartConditions();
        }

        /// <summary>
        ///     Performs the initial Map load
        /// </summary>
        protected override void Start()
        {
            Assert.IsNotNull(MainCamera);
            Assert.IsNotNull(ServerManager);
            Assert.IsNotNull(Avatar);
            base.Start();

            if (BuildingsRoofMaterials.Count == 0
                || BuildingsWallMaterials.Count == 0
                || BuildingsRoofMaterials.Count != BuildingsWallMaterials.Count)
            {
                throw new System.Exception("We expect at least one wall " +
                                           "and one roof material.");
            }
        }

        /// <summary>
        ///     Triggered by the UI when a new game needs to be created.
        ///     This event listener resets player and world data.
        /// </summary>
        public void OnNewGame()
        {
            _gameStarted = false;
            _startupCheckList = new List<string> {PLAYER_DATA_INITIALIZED, MAP_INITIALIZED};

            // Load and initialize Player Data
            PlayerService.GetInstance().Init(ServerManager.GetPlayerData());
            _startupCheckList.Remove(PLAYER_DATA_INITIALIZED);

            // Reload Maps
            LoadMap();
        }

        public void OnShowWorld()
        {
            Avatar.gameObject.SetActive(true);
            GetComponent<DynamicMapsUpdater>().enabled = true;
            MainCamera.enabled = true;
        }

        // We consider that the game is loaded when:
        // Reference data and player data are initialized
        // and the Map region is loaded.
        private void CheckStartConditions()
        {
            if (_startupCheckList.Count == 0 && !_gameStarted)
            {
                GameReady?.Invoke();
                _gameStarted = true;
            }
        }

        /// <summary>
        ///     Initializes the style options for this game, by setting materials to roads, buildings
        ///     and water areas.
        /// </summary>
        protected override void InitStylingOptions()
        {
            _zoinkiesStylesOptions = DefaultStyles.DefaultGameObjectOptions;

            // The default maps shader has a glossy property that allows the sky to reflect on it.
            Material waterMaterial =
                DefaultStyles.DefaultGameObjectOptions.RegionStyle.FillMaterial;
            waterMaterial.color = new Color(0.4274509804f, 0.7725490196f, 0.8941176471f);

            _zoinkiesStylesOptions.ModeledStructureStyle = new ModeledStructureStyle.Builder
            {
                Material = ModeledBuildingsMaterial
            }.Build();

            _zoinkiesStylesOptions.RegionStyle = new RegionStyle.Builder
            {
                FillMaterial = GroundMaterial
            }.Build();

            _zoinkiesStylesOptions.AreaWaterStyle = new AreaWaterStyle.Builder
            {
                FillMaterial = waterMaterial
            }.Build();

            _zoinkiesStylesOptions.LineWaterStyle = new LineWaterStyle.Builder
            {
                Material = waterMaterial
            }.Build();

            _zoinkiesStylesOptions.SegmentStyle = new SegmentStyle.Builder
            {
                Material = RoadsMaterial
            }.Build();

            if (RenderingStyles == null)
            {
                RenderingStyles = _zoinkiesStylesOptions;
            }
        }

        /// <summary>
        ///     Adds some squashing behavior to all extruded structures.
        ///     Basically, we squash everything around our Avatar so that generated game items can be seen
        ///     from a distance.
        /// </summary>
        protected override void InitEventListeners()
        {
            base.InitEventListeners();

            if (MapsService == null)
            {
                return;
            }

            // Apply a pre-creation listener that picks a random style for extruded buildings
            MapsService.Events.ExtrudedStructureEvents.WillCreate.AddListener(
                e =>
                {
                    int i = Random.Range(0, BuildingsRoofMaterials.Count);
                    e.Style = new ExtrudedStructureStyle.Builder
                    {
                        RoofMaterial = BuildingsRoofMaterials[i],
                        WallMaterial = BuildingsWallMaterials[i]
                    }.Build();
                });

            // Apply a pre-creation listener that picks a random style for modeled buildings
            // In this game, modeled buildings are plain and unicolor.
            MapsService.Events.ModeledStructureEvents.WillCreate.AddListener(
                e =>
                {
                    int i = Random.Range(0, BuildingsRoofMaterials.Count);
                    e.Style = new ModeledStructureStyle.Builder
                    {
                        Material = BuildingsRoofMaterials[i]
                    }.Build();
                });

            // Apply a post-creation listener that adds the squashing MonoBehaviour
            // to each building.
            MapsService.Events.ExtrudedStructureEvents.DidCreate.AddListener(
                e => { AddSquasher(e.GameObject); });

            // Apply a post-creation listener that adds the squashing MonoBehaviour
            // to each building.
            MapsService.Events.ModeledStructureEvents.DidCreate.AddListener(
                e => { AddSquasher(e.GameObject); });

            // Apply a post-creation listener that move road segments up to prevent  .
            MapsService.Events.SegmentEvents.DidCreate.AddListener(
                e =>
                {
                    // Move y position up to prevent z-fighting with water areas;
                    e.GameObject.transform.position += new Vector3(0f, 0.01f, 0f);
                });

            MapsService.Events.MapEvents.Loaded.AddListener(arg0 =>
            {
                if (!_gameStarted)
                {
                    _startupCheckList.Remove(MAP_INITIALIZED);
                    CheckStartConditions();
                }
            });
        }

        /// <summary>
        ///     Adds a Squasher MonoBehaviour to the supplied GameObject.
        /// </summary>
        /// <remarks>
        ///     The Squasher MonoBehaviour reduced the vertical scale of the GameObject's transform
        ///     when a building object is nearby.
        /// </remarks>
        /// <param name="go">The GameObject to which to add the Squasher behaviour.</param>
        private void AddSquasher(GameObject go)
        {
            Squasher squasher = go.AddComponent<Squasher>();
            squasher.Target = Avatar.transform;
            squasher.Near = SquashNear;
            squasher.Far = SquashFar;
            squasher.MaximumSquashing = MaximumSquash;
        }
    }
}

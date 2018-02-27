// Copyright 2017 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Hydrography;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Foundation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArcGISRuntime.Samples.Managers;
using Esri.ArcGISRuntime.Data;
using UIKit;

namespace ArcGISRuntime.Samples.SelectEncFeatures
{
    [Register("SelectEncFeatures")]
	[ArcGISRuntime.Samples.Shared.Attributes.OfflineData("a490098c60f64d3bbac10ad131cc62c7")]
    [ArcGISRuntime.Samples.Shared.Attributes.Sample(
        "Select ENC features",
        "Hydrography",
        "This sample demonstrates how to select an ENC feature.",
        "This sample automatically downloads ENC data from ArcGIS Online before displaying the map.")]
    public class SelectEncFeatures : UIViewController
    {
        // Create and hold reference to the used MapView
        private MapView _myMapView = new MapView();

        public SelectEncFeatures()
        {
            Title = "Select ENC features";
        }

        public override void ViewDidLoad()
        {
            CreateLayout();
            Initialize();

            base.ViewDidLoad();
        }

        private async void Initialize()
        {
            // Initialize the map with an oceans basemap
            _myMapView.Map = new Map(Basemap.CreateOceans());

            // Get the path to the ENC Exchange Set
            string encPath = await GetEncPath();

            // Store a list of data set extent's - will be used to zoom the mapview to the full extent of the Exchange Set
            List<Envelope> dataSetExtents = new List<Envelope>();

            // Create the cell and layer
            EncLayer myEncLayer = new EncLayer(new EncCell(encPath));

            // Add the layer to the map
            _myMapView.Map.OperationalLayers.Add(myEncLayer);

            // Wait for the layer to load
            await myEncLayer.LoadAsync();

            // Set the viewpoint
            _myMapView.SetViewpoint(new Viewpoint(myEncLayer.FullExtent));

            // Subscribe to tap events (in order to use them to identify and select features)
            _myMapView.GeoViewTapped += MyMapView_GeoViewTapped;
        }

        private void CreateLayout()
        {
            // Add MapView to the page
            View.AddSubviews(_myMapView);
        }

        public override void ViewDidLayoutSubviews()
        {
            // Setup the visual frame for the MapView
            _myMapView.Frame = new CoreGraphics.CGRect(0, 0, View.Bounds.Width, View.Bounds.Height);

            base.ViewDidLayoutSubviews();
        }

        private void ClearAllSelections()
        {
            // For each layer in the operational layers that is an ENC layer
            foreach (EncLayer layer in _myMapView.Map.OperationalLayers.OfType<EncLayer>())
            {
                // Clear the layer's selection
                layer.ClearSelection();
            }

            // Clear the callout
            _myMapView.DismissCallout();
        }

        private async void MyMapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            // First clear any existing selections
            ClearAllSelections();

            // Perform the identify operation
            IReadOnlyList<IdentifyLayerResult> results = await _myMapView.IdentifyLayersAsync(e.Position, 5, false);

            // Return if there are no results
            if (results.Count < 1) { return; }

            // Get the results that are from ENC layers
            IEnumerable<IdentifyLayerResult> encResults = results.Where(result => result.LayerContent is EncLayer);

            // Get the ENC results that have features
            IEnumerable<IdentifyLayerResult> encResultsWithFeatures = encResults.Where(result => result.GeoElements.Count > 0);

            // Get the first result with ENC features
            IdentifyLayerResult firstResult = encResultsWithFeatures.First();

            // Get the layer associated with this set of results
            EncLayer containingLayer = firstResult.LayerContent as EncLayer;

            // Get the first identified ENC feature
            EncFeature firstFeature = firstResult.GeoElements.First() as EncFeature;

            // Select the feature
            containingLayer.SelectFeature(firstFeature);

            // Create the callout definition
            CalloutDefinition definition = new CalloutDefinition(firstFeature.Acronym, firstFeature.Description);

            // Show the callout
            _myMapView.ShowCalloutAt(e.Location, definition);
        }

        private async Task<String> GetEncPath()
        {
            return DataManager.GetDataFolder("a490098c60f64d3bbac10ad131cc62c7", "GB5X01NW.000");
        }
    }
}
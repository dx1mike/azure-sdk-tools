﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Commands.Store
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;
    using System.Security.Permissions;
    using Commands.Utilities.Common;
    using Microsoft.WindowsAzure.Commands.Utilities.Properties;
    using Commands.Utilities.Store;
    using Microsoft.WindowsAzure.ServiceManagement;

    /// <summary>
    /// Gets all available Windows Azure add-ons from Marketplace and gets user purchased add-ons.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "AzureStoreAddOn"), OutputType(typeof(List<WindowsAzureAddOn>), typeof(List<WindowsAzureOffer>))]
    public class GetAzureStoreAddOnCommand : ServiceManagementBaseCmdlet
    {
        const string ListAvailableParameterSet = "ListAvailable";

        const string GetAddOnParameterSet = "GetAddOn";

        public StoreClient StoreClient { get; set; }

        public MarketplaceClient MarketplaceClient { get; set; }

        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, 
            ParameterSetName = ListAvailableParameterSet, HelpMessage = "List available add-ons")]
        public SwitchParameter ListAvailable { get; set; }

        [Parameter(Position = 1, Mandatory = false, ValueFromPipelineByPropertyName = true, 
            ParameterSetName = ListAvailableParameterSet, HelpMessage = "Country code")]
        [ValidateCountryLength()]
        public string Country { get; set; }

        [Parameter(Position = 0, Mandatory = false, ValueFromPipelineByPropertyName = true, 
            ParameterSetName = GetAddOnParameterSet, HelpMessage = "Add-On name")]
        public string Name { get; set; }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public override void ExecuteCmdlet()
        {
            if (ListAvailable.IsPresent)
	        {
		        ListAvailableAddOns();
	        }
            else
	        {
                GetAddOn();
	        }
        }

        private void GetAddOn()
        {
 	        StoreClient = StoreClient ?? new StoreClient(
                CurrentAzureSubscription.SubscriptionId,
                ServiceEndpoint,
                CurrentAzureSubscription.Certificate,
                text => this.WriteDebug(text),
                Channel);
            List<WindowsAzureAddOn> addOns = StoreClient.GetAddOn(new AddOnSearchOptions(Name, null, null));
            WriteObject(addOns, true);
        }

        private void ListAvailableAddOns()
        {
            LocationList locations = Channel.ListLocations(CurrentAzureSubscription.SubscriptionId);
            MarketplaceClient = MarketplaceClient ?? 
                new MarketplaceClient(locations.Select<Location, string>(l => l.Name));

            WriteVerbose(Resources.GetAllAddOnsWaitMessage);
            List<WindowsAzureOffer> result = MarketplaceClient.GetAvailableWindowsAzureOffers(Country);
            List<WindowsAzureOffer> knownProviders = result.Where<WindowsAzureOffer>(
                o => MarketplaceClient.IsKnownProvider(o.ProviderId)).ToList<WindowsAzureOffer>();
            WriteObject(knownProviders, true);
        }
    }
}
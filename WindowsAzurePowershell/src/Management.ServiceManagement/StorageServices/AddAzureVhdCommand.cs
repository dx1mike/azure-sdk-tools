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

namespace Microsoft.WindowsAzure.Management.ServiceManagement.StorageServices
{
    using System;
    using System.IO;
    using System.Management.Automation;
    using Cmdlets.Common;
    using Model;
    using Sync.Download;
    using WindowsAzure.ServiceManagement;


    /// <summary>
    /// Uploads a vhd as fixed disk format vhd to a blob in Windows Azure Storage
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "AzureVhd"), OutputType(typeof(VhdUploadContext))]
    public class AddAzureVhdCommand : ServiceManagementBaseCmdlet
    {
        private const int DefaultNumberOfUploaderThreads = 8;

        public AddAzureVhdCommand()
        {
        }

        public AddAzureVhdCommand(IServiceManagement channel)
        {
            Channel = channel;
        }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName="Vhd", HelpMessage = "Uri to blob")]
        [ValidateNotNullOrEmpty]
        [Alias("dst")]
        public Uri Destination
        {
            get;
            set;
        }

        [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName="Vhd", HelpMessage = "Local path of the vhd file")]
        [ValidateNotNullOrEmpty]
        [Alias("lf")]
        public FileInfo LocalFilePath
        {
            get;
            set;
        }

        private int numberOfUploaderThreads = DefaultNumberOfUploaderThreads;
        
        [Parameter(Position = 3, Mandatory = false, ValueFromPipelineByPropertyName = true, ParameterSetName = "Vhd", HelpMessage = "Number of uploader threads")]
        [ValidateNotNullOrEmpty]
        [ValidateRange(1, 64)]
        [Alias("th")]
        public int NumberOfUploaderThreads
        {
            get { return this.numberOfUploaderThreads; }
            set { this.numberOfUploaderThreads = value; }
        }

        [Parameter(Position = 4, Mandatory = false, ValueFromPipelineByPropertyName = true, ParameterSetName="Vhd", HelpMessage = "Uri to a base image in a blob storage account to apply the difference")]
        [ValidateNotNullOrEmpty]
        [Alias("bs")]
        public Uri BaseImageUriToPatch
        {
            get;
            set;
        }

        [Parameter(Position = 5, Mandatory = false, ValueFromPipelineByPropertyName = true, ParameterSetName="Vhd", HelpMessage = "Delete the blob if already exists")]
        [ValidateNotNullOrEmpty]
        [Alias("o")]
        public SwitchParameter OverWrite
        {
            get;
            set;
        }


        public UploadParameters ValidateParameters()
        {
            BlobUri destinationUri;
            if (!BlobUri.TryParseUri(Destination, out destinationUri))
            {
                throw new ArgumentOutOfRangeException("Destination", this.Destination.ToString());
            }

            BlobUri baseImageUri = null;
            if (this.BaseImageUriToPatch != null)
            {
                if (!BlobUri.TryParseUri(BaseImageUriToPatch, out baseImageUri))
                {
                    throw new ArgumentOutOfRangeException("BaseImageUriToPatch", this.BaseImageUriToPatch.ToString());
                }

                if (!String.IsNullOrEmpty(destinationUri.Uri.Query))
                {
                    var message = String.Format("SAS Uri for the destination blob is not supported in patch mode:{0}", destinationUri.Uri);
                    throw new ArgumentOutOfRangeException("Destination", message);
                }
            }

            var parameters = new UploadParameters(destinationUri, baseImageUri, LocalFilePath, OverWrite.IsPresent, NumberOfUploaderThreads)
            {
                Cmdlet = this,
                BlobObjectFactory = new CloudPageBlobObjectFactory(this.Channel, this.CurrentSubscription.SubscriptionId, TimeSpan.FromMinutes(1))
            };

            return parameters;
        }

        protected override void OnProcessRecord()
        {
            var parameters = ValidateParameters();
            var vhdUploadContext = VhdUploaderModel.Upload(parameters);
            WriteObject(vhdUploadContext);
        }
    }
}

#region License

// Copyright (C) 2022 Reetus
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY

#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using ClassicAssist.Shared.Resources;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace ClassicAssist.Data.Backup.GoogleDrive
{
    public class GoogleDriveClient
    {
        // Google Drive backup requires an OAuth 2.0 desktop-app client ID/secret.
        // Upstream ClassicAssist ships Reetus's own credentials hardcoded here so
        // the feature works out-of-the-box; this Avalonia port intentionally does
        // NOT redistribute those (quota, abuse, and licensing concerns). To use
        // Google Drive backup with this fork you must register your own OAuth
        // client at https://console.cloud.google.com/apis/credentials (Application
        // type: Desktop app) and paste the values below, or wire them up via a
        // local-only config mechanism of your choosing.
        private const string CLIENT_ID = "";
        private const string CLIENT_SECRET = "";

        public static UserCredential Credential { get; set; }

        public static async Task<UserCredential> GetAccessTokenAsync( CancellationToken cancellationToken )
        {
            if ( string.IsNullOrEmpty( CLIENT_ID ) || string.IsNullOrEmpty( CLIENT_SECRET ) )
            {
                throw new InvalidOperationException(
                    "Google Drive backup is not configured in this build. " +
                    "Set CLIENT_ID and CLIENT_SECRET in GoogleDriveClient.cs to your own " +
                    "OAuth 2.0 desktop-app credentials from https://console.cloud.google.com/apis/credentials." );
            }

            GoogleDriveDataStore dataStore = GoogleDriveDataStore.GetInstance();

            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets { ClientId = CLIENT_ID, ClientSecret = CLIENT_SECRET },
                new[] { DriveService.Scope.DriveFile }, AssistantOptions.UserId, cancellationToken, dataStore );

            Credential = credential ?? throw new Exception( Strings.Authentication_error_or_timeout );

            return credential;
        }

        public static async Task LogoutAsync()
        {
            await GoogleDriveDataStore.GetInstance().ClearAsync();
        }

        public static async Task<DriveService> GetServiceClient()
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter( TimeSpan.FromMinutes( 2 ) );

            if ( Credential == null )
            {
                await GetAccessTokenAsync( tokenSource.Token );
            }

            DriveService service = new DriveService( new BaseClientService.Initializer
            {
                HttpClientInitializer = Credential, ApplicationName = "ClassicAssist"
            } );

            return service;
        }
    }
}
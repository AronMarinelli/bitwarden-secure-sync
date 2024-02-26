# Bitwarden Secure Sync

A simple tool that can be used to export your Bitwarden vault to a local file periodically.

Uses the [Bitwarden CLI](https://github.com/bitwarden/clients) tool to communicate with the API, and exports your passwords using the default Bitwarden export method.

> I am not affiliated with Bitwarden Inc. in any way, and am providing this software as-is. This project is intended for personal use, and might receive breaking updates without notice. 

## Usage
### Docker (recommended)
You can find the docker image for this repository here: https://hub.docker.com/r/aronmarinelli/bitwarden-secure-sync

It is important to bind both the `/app/config` and `/app/data` directories for the container to function properly. The data directory can be configured through the appsettings.json file, but it is recommended to leave this as-is when running inside of Docker.

Upon initial run, the application shall automatically create an appsettings.json file at the /app/config path. In order for the application to function, the appsettings.json file should be configured as [described below](#configuration).

### .NET/Console app
It is possible to run the application outside of Docker. 

For this, you must clone the repo to your local development environment and build the application yourself.

In order for this build to function properly, an appsettings.json file should be present either within the working directory or within a config subdirectory. See [below](#configuration) for further information.

## Configuration
An example appsettings.json file is included in the repository, and will also be created upon initial run.

### Example
```json
{
  "Bitwarden": {
    "ClientId": "user.00000000-0000-0000-0000-000000000000",
    "ClientSecret": "your-secret-here",
    "ServerUrl": "https://vault.bitwarden.com",
    "VaultPassword": "your-vault-password-here"
  },
  "Sync": {
    "CronSchedule": "0 0 * * *",
    "RunOnStartup": true,
    "IncludeOrganisationItems": false,
    "EncryptUsingCustomKey": false,
    "EncryptionKey": null,
    "FileRetention": 7,
    "DataDirectory": "data"
  }
}
```

### Obtaining your Bitwarden API credentials
For the Bitwarden CLI application to communicate with the API, it is required that the application is aware of your API credentials. These credentials can be found on the [Keys](https://vault.bitwarden.com/#/settings/security/security-keys) tab within the Bitwarden account settings page. 

See [this page](https://bitwarden.com/help/personal-api-key/) for more information regarding API authentication.

### Configuration properties
`Bitwarden:ClientId`

This property should contain the client ID for the Bitwarden account that is to be exported.

`Bitwarden:ClientSecret`

This property should contain the client secret for the Bitwarden account that is to be exported.

`Bitwarden:ServerUrl`

This property should contain the server URL for the Bitwarden account that is to be exported. 

At the time of writing, Bitwarden Inc. hosts two vault endpoints. An international version at https://vault.bitwarden.com/, and a European version at https://vault.bitwarden.eu/.
If you host your vault yourself (using e.g. [Vaultwarden](https://github.com/dani-garcia/vaultwarden)), you should enter your personal vault URL here. Do note that this is not a tested scenario, and any compatibility issues that arise are not supported by the Bitwarden CLI team.

`Bitwarden:VaultPassword`

This property should contain the vault password for the Bitwarden account that is to be exported.

`Sync:CronSchedule`

This property should contain the CRON schedule on which the export should run, by default this is set to 0 0 * * * (once every day at 00:00).

`Sync:RunOnStartup`

This property indicates whether to run an initial export upon startup, or whether to just use the regular schedule. Defaults to true.

`Sync:IncludeOrganisationItems`

This property indicates whether the tool should attempt to export linked organisation data from your Bitwarden account. Depending on configuration from within your organisation, this process might fail. Defaults to false.

`Sync:EncryptUsingCustomKey`

This property indicates whether the tool should encrypt the export using a custom key provided within the configuration, or whether to use the default account key from Bitwarden. Using the default account key restricts the restoring of the created export to the original Bitwarden account the export was created from. Defaults to false.

`Sync:EncryptionKey`

This property should contain the custom encryption key, if enabled through the property above.

`Sync:FileRetention`

This property indicates how many backup sets to keep before purging the oldest one. Can be set to NULL in order to retain backups forever. Defaults to 7.

`Sync:DataDirectory`

This property allows you to specify the directory where backups are created. Defaults to "data".

For Docker installations, the default value should not be altered under normal circumstances (instead, you should mount the /app/data directory to the desired location).
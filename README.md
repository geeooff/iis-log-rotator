# IIS Log Rotator
Microsoft Internet Information Services Log Rotation program.

---

## Features

1. Autodetects installed IIS services and file logging settings
2. Compress files older than _N_-days
3. Delete files older than _N_-days
4. Default settings can be overridden per-site
5. Reports in Windows Event Log
6. _Simulation mode_ (or _what if..._) can be used

## Supported IIS versions

* IIS 5.0
* IIS 5.1
* IIS 6.0
* IIS 7.0
* IIS 7.5
* IIS 8.0
* IIS 8.5
* IIS 10.0

## Supported IIS services

* HTTP
* FTP
* SMTP
* NNTP

Web Management Service (WMSvc) logs are __not yet__ supported.

## System requirements

* Microsoft .NET Framework 4.5
* Windows Server 2008 / Vista only: IIS 6 Metabase Compatibility feature
* Administrator rights (because of WMI and IIS Metabase readings)

---

## How to run

1. Extract the program to a local folder
2. Execute `IisLogRotator.exe /s` to see what logfiles will be compressed or removed
3. Execute `IisLogRotator.exe` to compress or delete logfiles definitively

## How to install / schedule

If you need to execute the program daily and have reports to Windows Event Log,
you should register it to Windows Task Scheduler.

1. Execute `install.cmd` with Administrator rights
2. Modify scheduled task named `IIS Logs Rotation` if not satisfied with default settings (every day at 1:00 am UTC)

## How to uninstall / unschedule

* To remove everything, execute `uninstall.cmd` with Administrator rights
* To disable the scheduled task, disable Windows scheduled task named `IIS Logs Rotation`, or execute `SCHTASKS /Change /TN "IIS Logs Rotation" /DISABLE`

---

## Settings

Open the `IisLogRotator.config` file with an XML-aware editor (like [Visual Studio Code](http://code.visualstudio.com), [Sublime Text](http://www.sublimetext.com)) to avoid errors.

### Defaults

Default settings are set on the `/configuration/rotation/defaultsSettings` XML node.

Example for compression after 7 days, and deletion after 2 years :

```xml
<rotation>
	<defaultSettings compress="true" compressAfter="7" delete="true" deleteAfter="730"/>
</rotation>
```

### Site-specific settings

To specify site-specific settings, you have to find its service code and ID,
then add to your settings in a `siteSettings/siteSettings` node with the ID set to the `id` attribute.

Example for `W3SVC1` Website settings, to do deletion only after 30 days :

```xml
<rotation>
	<defaultSettings compress="true" compressAfter="7" delete="true" deleteAfter="730"/>
	<siteSettings>
		<siteSettings id="W3SVC1" compress="false" delete="true" deleteAfter="30"/>
	</siteSettings>
</rotation>
```

### `defaultSettings` / `siteSettings` allowed values

| Attribute | Values |
| --- | --- |
| `compress` | `true` to enable compression or `false` to disable |
| `compressAfter` | Number of days (must be greater than zero) |
| `delete` | `true` to enable deletion or `false` to disable |
| `deleteAfter` | Number of days (must be greater than `compressAfter` if `delete` and `compress` are `true`) |
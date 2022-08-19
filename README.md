# Bad Windows Service

This project is an insecurely implemented and configured Windows service, vulnerable to the following attacks:

* Hijack Execution Flow: DLL Search Order Hijacking ([T1574.001](https://attack.mitre.org/techniques/T1574/001/).
* Hijack Execution Flow: Path Interception by PATH Environment Variable ([T1574.007](https://attack.mitre.org/techniques/T1574/007/).
* Hijack Execution Flow: Path Interception by Search Order Hijacking ([T1574.008](https://attack.mitre.org/techniques/T1574/008/).
* Hijack Execution Flow: Path Interception by Unquoted Path ([T1574.009](https://attack.mitre.org/techniques/T1574/009/).
* Hijack Execution Flow: Services File Permissions Weakness ([T1574.010](https://attack.mitre.org/techniques/T1574/010/).
* Hijack Execution Flow: Services Registry Permissions Weakness ([T1574.011](https://attack.mitre.org/techniques/T1574/011/).

The Installer project installs the service in the designated path with some of the above misconfigurations, and must be launched in an elevated context.

The BadWindowsService project is implements a a service with some of the above vulnerabilities.

Exploitation of this service results in local elevation of privileges to the security context of LocalSystem.

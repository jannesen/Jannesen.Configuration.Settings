# Jannesen.Service

With this library it is easy to implement .net Windows Service.

## This library implments
- The executable can be started / stopped by de Windows service control manager.
- The executable can by running from commandline (for testing)
- (debug) log facility to write (debug) logging to log files. Logfile are placed in a special log directory. With 1 log file per day.
- installer and uninstaller.
  The installer not only creates the services it can also create a database access for the service, Registrate a http url, Registrate firewall rules and set te correct rights to files so that te service has the correct access to the files.

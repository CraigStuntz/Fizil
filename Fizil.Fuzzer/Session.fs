module Session

open Log
open Project
open Status

type Session = {
    Logger: Logger
    Project: DumbProject
    StatusMonitor: StatusMonitor
}


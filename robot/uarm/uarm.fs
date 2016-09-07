namespace Microsoft.Robotics.UArm

open System.Threading
open Microsoft.Robotics.Brief
open Microsoft.Robotics.Tests.Reflecta

type UArm (port: string) =
    let mutable fast = false
    let mutable _x, _y, _r, _t, _z = 0., 0., 0., 0., 0.
    let compiler = new Compiler()
    let (reflecta : ReflectaClient option ref) = ref None
    let send execute bytecode =
        match !reflecta with
        | Some client ->
            try
                client.SendFrame(Array.append bytecode [|(if execute then 0uy else 1uy)|])
            with ex ->
                printfn "Communication error: %s" ex.Message
                reflecta := None
        | None -> () // not connected to MCU
    let rec processor = MailboxProcessor.Start(fun box -> async {
        while true do
            let! brief, wait = box.Receive()
            printfn "SEND BRIEF: %s (Queue %i, Wait %i)" brief processor.CurrentQueueLength wait
            compiler.EagerCompile(brief) |> fst |> send true
            do! Async.Sleep wait } )
    let exec wait brief =
        printfn "EXEC BRIEF: %s" brief
        processor.Post(brief, wait)
        Thread.Sleep(10)
    let dist3D a a' b b' c c' =
        let sq x = x * x
        sqrt (sq (a' - a) + sq (b' - b) + sq (c' - c))
    member this.Fast(setting) = fast <- setting
    member this.Connect() =
        compiler.Reset()
        ["beep",   100uy
         "grip",   101uy; "release", 102uy
         "attach", 103uy; "detach",  104uy
         "joint?", 105uy; "joint!",  106uy; "joints!", 107uy; "backlash", 111uy
         "xyz?",   108uy; "xyz!",    109uy; "xyz!!",   110uy; "rtz!!",    113uy]
         |> List.iter compiler.Instruction
        let client = new ReflectaClient(port)
        client.ErrorReceived.Add(fun e -> printfn "Error: %s" e.Message)
        client.FrameReceived.Add(fun e -> printfn "Frame: %A" e.Frame)
        exec 500 "(reset)" // wait is to give time for Brief engine to start
        reflecta := Some client
    member this.Disconnect() =
        match !reflecta with
        | Some client -> client.Dispose(); reflecta := None
        | None -> () // not connected
    member this.ZeroG(zero: bool) = exec 100 (if zero then "detach" else "attach")
    member this.Grip(grip: bool) = exec 1000 (if grip then "grip" else "release")
    member this.Move(x: float, y: float, z: float) =
        let dist = (dist3D _x x _y y _z z) * 10000. |> int
        _x <- x; _y <- y; _z <- z
        printfn "Move: %f %f %f (%i)" x y z dist
        let xx = int (x * 10000.) + 22000
        let yy = int (y * 10000.)
        let zz = int (z * 10000.) + 5000
        let wait = dist / if fast then 10 else 5
        sprintf "%i %i %i 3000 xyz!!" xx yy zz |> exec wait
    member this.MoveRTZ(r: float, t: float, z: float) =
        let dist = (dist3D _r r _t t _z z) * 10000. |> int
        _r <- r; _t <- t; _z <- z
        printfn "MoveRTZ: %f %f %f" r t z
        let rr = int (r * 10000.) + 22000
        let tt = int (t * 650.) + 1800
        let zz = int (z * 10000.) + 5000
        let wait = dist / if fast then 10 else 5
        sprintf "%i %i %i 3000 rtz!!" rr tt zz |> exec wait
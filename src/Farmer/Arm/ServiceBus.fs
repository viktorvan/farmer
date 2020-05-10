[<AutoOpen>]
module Farmer.Arm.ServiceBus

open Farmer
open System

type ServiceBusQueue =
    { Name : ResourceName
      LockDurationMinutes : int option
      DuplicateDetectionHistoryTimeWindowMinutes : int option
      Session : bool option
      DeadLetteringOnMessageExpiration : bool option
      MaxDeliveryCount : int option
      EnablePartitioning : bool option
      DependsOn : ResourceName list }

type Namespace =
    { Name : ResourceName
      Location : Location
      Sku : ServiceBusSku
      Queues :ServiceBusQueue list
      DependsOn : ResourceName list }
    member this.Capacity =
        match this.Sku with
        | ServiceBusSku.Basic -> None
        | ServiceBusSku.Standard -> None
        | ServiceBusSku.Premium OneUnit -> Some 1
        | ServiceBusSku.Premium TwoUnits -> Some 2
        | ServiceBusSku.Premium FourUnits -> Some 4
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| ``type`` = "Microsoft.ServiceBus/namespaces"
               apiVersion = "2017-04-01"
               name = this.Name.Value
               location = this.Location.ArmValue
               sku =
                    {| name = string this.Sku
                       tier = string this.Sku
                       capacity = this.Capacity |> Option.toNullable |}
               dependsOn = this.DependsOn |> List.map (fun r -> r.Value)
               resources =
                 [ for queue in this.Queues do
                     {| apiVersion = "2017-04-01"
                        name = queue.Name.Value
                        ``type`` = "Queues"
                        dependsOn = queue.DependsOn |> List.map (fun r -> r.Value)
                        properties =
                         {| lockDuration = queue.LockDurationMinutes |> Option.map (sprintf "PT%dM") |> Option.toObj
                            requiresDuplicateDetection =
                                match queue.DuplicateDetectionHistoryTimeWindowMinutes with
                                | Some _ -> Nullable true
                                | None -> Nullable()
                            duplicateDetectionHistoryTimeWindow =
                                queue.DuplicateDetectionHistoryTimeWindowMinutes
                                |> Option.map (sprintf "PT%dM")
                                |> Option.toObj
                            requiresSession = queue.Session |> Option.toNullable
                            deadLetteringOnMessageExpiration = queue.DeadLetteringOnMessageExpiration |> Option.toNullable
                            maxDeliveryCount = queue.MaxDeliveryCount |> Option.toNullable
                            enablePartitioning = queue.EnablePartitioning |> Option.toNullable |}
                     |}
                 ]
            |} :> _

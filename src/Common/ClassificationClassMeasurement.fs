namespace BucklingSprings.Aware.Common.UserConfiguration

open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.Models

module ClassificationByClassMeasurement =

    let filteredMeasurement (cf  : ClassificationClassFilter) (total : Unit -> 'a) (forClass : ClassIdentifier -> 'a)  : MeasureByClass<'a> list =
        match cf with
        | ClassificationClassFilter.NoFilter -> [MeasureByClass.TotalAcrossClasses (total ())]
        | ClassificationClassFilter.FilterToClasses cls -> cls |> List.map (fun c -> MeasureByClass.ForClass(ClassIdentifier c.id, forClass (ClassIdentifier c.id)))
        
    let zeros (cf : ClassificationClassFilter) (z : 'a) : MeasureByClass<'a> list = filteredMeasurement cf (fun _ -> z) (fun _ -> z)

    let filter (cf  : ClassificationClassFilter) (xs : MeasureByClass<'a> list)  : MeasureByClass<'a list> list =
        match cf with
        | ClassificationClassFilter.NoFilter -> [MeasureByClass.TotalAcrossClasses (xs |> List.choose ByClass.chooseTotal)]
        | ClassificationClassFilter.FilterToClasses cls -> cls |> List.map (fun c -> MeasureByClass.ForClass((ClassIdentifier c.id), xs |> List.choose (ByClass.chooseForClass (ClassIdentifier c.id))))

    let filterMapForClasses (cls : ClassificationClass list) (mapping : 'a list -> 'b) (xs : MeasureByClass<'a> list) : (ClassIdentifier * 'b) list  =
        cls |> List.map (fun c -> (ClassIdentifier c.id, xs |> List.choose (ByClass.chooseForClass (ClassIdentifier c.id)) |> mapping))
    
    let filterMap (cf  : ClassificationClassFilter) (mapping : 'a list -> 'b) (xs : MeasureByClass<'a> list)  : MeasureByClass<'b> list =
        match cf with
        | ClassificationClassFilter.NoFilter -> [MeasureByClass.TotalAcrossClasses (xs |> List.choose ByClass.chooseTotal |> mapping)]
        | ClassificationClassFilter.FilterToClasses cls -> cls |> List.map (fun c -> MeasureByClass.ForClass(ClassIdentifier c.id, xs |> List.choose (ByClass.chooseForClass (ClassIdentifier c.id)) |> mapping))


module ClassificationForClassMeasurement =
    
    let filterMapForClasses (cls : ClassificationClass list) (mapping : 'a list -> 'b) (xs : MeasureForClass<'a> list) : MeasureForClass<'b> list  =
        cls |> List.map (fun c -> (ClassIdentifier c.id, xs |> List.choose (ForClass.chooseForClass (ClassIdentifier c.id)) |> mapping))

    let createLimitToClassesFilter (cls : ClassificationClass list) : MeasureForClass<'a> -> bool =
        let classIds = Set(cls |> List.map (fun c -> ClassIdentifier c.id))
        let filter c =
            let classId, _ = c
            classIds.Contains(classId)
        filter
namespace BucklingSprings.Aware

open BucklingSprings.Aware.Core
open BucklingSprings.Aware.Core.Measurement
open BucklingSprings.Aware.Core.Summaries
open BucklingSprings.Aware.Core.Models
open BucklingSprings.Aware.Common.UserConfiguration
open BucklingSprings.Aware.Common.Focus


module LazyFocusSessions =

    let focusSessionsForClassifier (config : UserGlobalConfiguration) (lastNDaySamples : WithDate<MeasureForClass<ActivitySummary>> list) =
        lazy (
            let endDt = DataDateRangeFilterUtils.endDt config.dateRangeFilter
            let dtFilter = Dated.filterForDay endDt
            let classFilter = Dated.unWrap >> ClassificationForClassMeasurement.createLimitToClassesFilter (ClassificationClassFilterUtils.selectedClassesOrAll config.classification)
            
            let samples = 
                lastNDaySamples
                    |> (List.filter dtFilter)
                    |> (List.filter classFilter)

            FocusSessionCalculator.combineAndFlush (config.classification.idleMap) (DataDateRangeFilterUtils.isToday config.dateRangeFilter) samples
        )




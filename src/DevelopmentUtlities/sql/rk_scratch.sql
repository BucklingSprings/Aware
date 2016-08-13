use aware_dev;


select 'SELECT COUNT(*) FROM ' + TABLE_NAME from INFORMATION_SCHEMA.TABLES


select count(*) from ClassificationClasses where classifier_id = 1
select count(distinct className) from ClassificationClasses where classifier_id = 1

select className as Program, count(*) as Count
from ClassificationClasses
where classifier_id = 1
group by className
having count(*) > 1

select * from ClassificationClasses where className in ('Idle', 'VideoVisitation.UserInterface.Visitor', 'Windows GUI symbolic debugger') and classifier_id = 1

select * from SampleClassAssignments where classifierIdentifier = 1 and assignedClass_id in (24301, 24302)

select * from ActivitySamples where id in (select ActivitySample_id from SampleClassAssignments where classifierIdentifier = 1 and assignedClass_id in (24304, 24305, 24306, 24307, 24308))

select * from ActivityWindowDetails where id in (335660, 335661, 335662)

select distinct summary from StoredSummaries where summaryType = 'Day_SummarySetVersion_AcrossClasses'
select * from Classifiers


select top 10 className as Program
from ClassificationClasses
where classifier_id = 1
Order By id desc

select top 10 * from ClassifierModelExamples Order By model_id desc;
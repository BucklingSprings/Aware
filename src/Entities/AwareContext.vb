Imports System.Data.Entity
Imports BucklingSprings.Aware.Core.Models
Imports System.Data.Entity.Infrastructure

' ReSharper disable CheckNamespace
Namespace BucklingSprings.Aware.Entitities
	' ReSharper restore CheckNamespace

	Public Class AwareContextContextFactory
		Implements IDbContextFactory(Of AwareContext)

		Public Function Create() As AwareContext Implements IDbContextFactory(Of AwareContext).Create
			Return New AwareContext(Core.Environment.connectionString)
		End Function
	End Class

	Public Class AwareContext
		Inherits DbContext

		Public Sub New(nameOrConnectionString As String)
			MyBase.New(nameOrConnectionString)
		End Sub

		Public Property ActivitySamples As DbSet(Of ActivitySample)
		Public Property Processes As DbSet(Of BucklingSprings.Aware.Core.Models.Process)
		Public Property WindowDetails As DbSet(Of ActivityWindowDetail)
		Public Property SampleClassAssignments As DbSet(Of SampleClassAssignment)
		Public Property Classifiers As DbSet(Of Classifier)
		Public Property Classes As DbSet(Of ClassificationClass)
		Public Property CategoryClassifierPhrases As DbSet(Of CategoryClassifierPhrase)
		Public Property Summaries As DbSet(Of StoredSummary)
		Public Property Goals As DbSet(Of StoredGoal)
		Public Property Configurations As DbSet(Of Configuration)
		Public Property ClassifierModels As DbSet(Of ClassifierModel)
		Public Property ClassifierModelExamples As DbSet(Of ClassifierModelExample)


		Protected Overrides Sub OnModelCreating(ByVal b As DbModelBuilder)

			b.Entity(Of Classifier).Property(Function(c) c.classifierName).IsRequired()
			b.Entity(Of Classifier).Property(Function(c) c.classifierType).IsRequired()
			b.Entity(Of Classifier).HasMany(Function(c) c.classes).WithRequired(Function(c) c.classifier)




			b.Entity(Of ClassificationClass).Property(Function(p) p.className).IsRequired()

			b.Entity(Of CategoryClassifierPhrase).Property(Function(p) p.phrase).IsRequired()
			b.Entity(Of CategoryClassifierPhrase).HasRequired(Function(p) p.classificationClass)


			b.Entity(Of ActivityWindowDetail).Property(Function(p) p.windowText).IsRequired()
			b.Entity(Of ActivityWindowDetail).HasRequired(Function(p) p.processInformation)


			b.Entity(Of ActivitySample).HasMany(Of SampleClassAssignment)(Function(a) a.classes).WithRequired().WillCascadeOnDelete(False)
			b.Entity(Of ActivitySample).Property(Function(s) s.sampleEndTimeAndDate).IsRequired()
			b.Entity(Of ActivitySample).Property(Function(s) s.sampleStartTimeAndDate).IsRequired()

			b.Entity(Of SampleClassAssignment).Property(Function(a) a.classifierDefinitionVersion).IsRequired()
			b.Entity(Of SampleClassAssignment).Property(Function(a) a.classifierIdentifier).IsRequired()
			b.Entity(Of SampleClassAssignment).HasRequired(Function(a) a.assignedClass)




			b.Entity(Of BucklingSprings.Aware.Core.Models.Process).Property(Function(p) p.processName).IsRequired()



			b.Entity(Of StoredSummary).Property(Function(p) p.summaryType).IsRequired()
			b.Entity(Of StoredSummary).Property(Function(p) p.summaryType).HasMaxLength(200)
			b.Entity(Of StoredSummary).Property(Function(p) p.summary).IsRequired()

			b.Entity(Of StoredGoal).Property(Function(g) g.startTime).IsRequired()
			b.Entity(Of StoredGoal).Property(Function(g) g.endTime).IsRequired()
			b.Entity(Of StoredGoal).Property(Function(g) g.comparison).IsRequired()
			b.Entity(Of StoredGoal).Property(Function(g) g.period).IsRequired()
			b.Entity(Of StoredGoal).Property(Function(g) g.value).IsRequired()
			b.Entity(Of StoredGoal).Property(Function(g) g.target).IsRequired()
			b.Entity(Of StoredGoal).HasOptional(Function(g) g.goalClass)

			b.Entity(Of Configuration).Property(Function(c) c.name).IsRequired()
			b.Entity(Of Configuration).Property(Function(c) c.value).IsRequired()


			b.Entity(Of ClassifierModelExample).HasRequired(Function(a) a.model)
			b.Entity(Of ClassifierModelExample).HasRequired(Function(a) a.windowDetail)
			b.Entity(Of ClassifierModelExample).Property(Function(g) g.trained).IsRequired()



		End Sub
	End Class

End Namespace


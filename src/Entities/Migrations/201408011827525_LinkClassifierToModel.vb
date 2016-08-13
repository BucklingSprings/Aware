Imports System
Imports System.Data.Entity.Migrations

Namespace BucklingSprings.Aware.Entitities
    Public Partial Class LinkClassifierToModel
        Inherits DbMigration
    
        Public Overrides Sub Up()
            AddColumn("dbo.Classifiers", "model_id", Function(c) c.Int())
            AddForeignKey("dbo.Classifiers", "model_id", "dbo.ClassifierModels", "id")
        End Sub
        
        Public Overrides Sub Down()
            DropIndex("dbo.Classifiers", New String() { "model_id" })
            DropForeignKey("dbo.Classifiers", "model_id", "dbo.ClassifierModels")
            DropColumn("dbo.Classifiers", "model_id")
        End Sub
    End Class
End Namespace

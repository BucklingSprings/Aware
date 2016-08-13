Imports System.Data.Entity.Migrations
Imports BucklingSprings.Aware.Core.Models

Namespace BucklingSprings.Aware.Entitities

    Public NotInheritable Class MigrationConfiguration
        Inherits DbMigrationsConfiguration(Of AwareContext)

        Public Sub New()
            AutomaticMigrationsEnabled = True
            AutomaticMigrationDataLossAllowed = True
        End Sub

        Protected Overrides Sub Seed(context As AwareContext)

            '  This method will be called after migrating to the latest version.

            '  You can use the DbSet(Of T).AddOrUpdate() helper extension method 
            '  to avoid creating duplicate seed data. E.g.
            '
            '    context.People.AddOrUpdate(
            '       Function(c) c.FullName,
            '       New Customer() With {.FullName = "Andrew Peters"},
            '       New Customer() With {.FullName = "Brice Lambson"},
            '       New Customer() With {.FullName = "Rowan Miller"})
        End Sub

    End Class

End Namespace

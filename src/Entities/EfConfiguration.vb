Imports System.Data.Entity
Imports System.Data.Entity.SqlServer

Namespace BucklingSprings.Aware.Entitities
	Public Class EfConfiguration
		Inherits DbConfiguration

		Public Sub New()
			SetProviderServices(SqlProviderServices.ProviderInvariantName, SqlProviderServices.Instance)
		End Sub

	End Class
End Namespace
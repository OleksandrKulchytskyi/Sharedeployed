namespace ShareDeployed.DataAccess.Migrations
{
	using System.Data.Entity.Migrations;

	public partial class RenameChatUsersToMessangerUsers : DbMigration
	{
		public override void Up()
		{
			RenameTable(name: "ChatUsers", newName: "MessangerUsers");
		}

		public override void Down()
		{
			RenameTable(name: "MessangerUsers", newName: "ChatUsers");
		}
	}
}

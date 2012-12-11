namespace ShareDeployed.DataAccess.Migrations
{
	using System.Data.Entity.Migrations;

	public partial class AddedUserReadMessage : DbMigration
	{
		public override void Up()
		{
			CreateTable(
				"UserReadMessage",
				c => new
					{
						User_Key = c.Int(nullable: false),
						Message_Key = c.Int(nullable: false),
					})
				.PrimaryKey(t => new { t.User_Key, t.Message_Key })
				.ForeignKey("MessangerUsers", t => t.User_Key, cascadeDelete: true)
				.ForeignKey("Messages", t => t.Message_Key, cascadeDelete: true)
				.Index(t => t.User_Key)
				.Index(t => t.Message_Key);
		}

		public override void Down()
		{
			DropIndex("UserReadMessage", new[] { "Message_Key" });
			DropIndex("UserReadMessage", new[] { "User_Key" });
			DropForeignKey("UserReadMessage", "Message_Key", "Messages");
			DropForeignKey("UserReadMessage", "User_Key", "MessangerUsers");
			DropTable("UserReadMessage");
		}
	}
}

namespace ShareDeployed.DataAccess.Migrations
{
	using System.Data.Entity.Migrations;

	public partial class AddAppAndResponseTables : DbMigration
	{
		public override void Up()
		{
			CreateTable(
				"Applications",
				c => new
					{
						Key = c.Int(nullable: false, identity: true),
						AppId = c.String(nullable: false),
						MachineName = c.String(),
						LastLoggedIn = c.DateTime(),
					})
				.PrimaryKey(t => t.Key);

			CreateTable(
				"MesageResponse",
				c => new
					{
						Key = c.Int(nullable: false),
						ResponseText = c.String(nullable: false, unicode: false, storeType: "text"),
						IsSent = c.Boolean(nullable: false),
					})
				.PrimaryKey(t => t.Key)
				.ForeignKey("Messages", t => t.Key, cascadeDelete: true)
				.Index(t => t.Key);

			AddColumn("Messages", "App_Key", c => c.Int());
			AddColumn("Messages", "ResponseKey", c => c.Int());
			AddForeignKey("Messages", "App_Key", "Applications", "Key");
			CreateIndex("Messages", "App_Key");
		}

		public override void Down()
		{
			DropIndex("MesageResponse", new[] { "Key" });
			DropIndex("Messages", new[] { "App_Key" });
			DropForeignKey("MesageResponse", "Key", "Messages");
			DropForeignKey("Messages", "App_Key", "Applications");
			DropColumn("Messages", "ResponseKey");
			DropColumn("Messages", "App_Key");
			DropTable("MesageResponse");
			DropTable("Applications");
		}
	}
}

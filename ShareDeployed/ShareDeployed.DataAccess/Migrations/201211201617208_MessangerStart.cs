namespace ShareDeployed.DataAccess.Migrations
{
	using System.Data.Entity.Migrations;

	public partial class MessangerStart : DbMigration
	{
		public override void Up()
		{
			CreateTable(
				"MessangerClients",
				c => new
					{
						Key = c.Int(nullable: false, identity: true),
						Id = c.String(),
						LastActivity = c.DateTimeOffset(nullable: false),
						UserAgent = c.String(),
						User_Key = c.Int(nullable: false),
					})
				.PrimaryKey(t => t.Key)
				.ForeignKey("ChatUsers", t => t.User_Key, cascadeDelete: true)
				.Index(t => t.User_Key);

			CreateTable(
				"ChatUsers",
				c => new
					{
						Key = c.Int(nullable: false, identity: true),
						Id = c.String(maxLength: 200),
						Name = c.String(),
						Hash = c.String(),
						Salt = c.String(),
						HashedPassword = c.String(),
						LastActivity = c.DateTime(nullable: false),
						LastNudged = c.DateTime(),
						Status = c.Int(nullable: false),
						Note = c.String(maxLength: 200),
						AfkNote = c.String(maxLength: 200),
						IsAfk = c.Boolean(nullable: false),
						Flag = c.String(maxLength: 2),
						Identity = c.String(),
						Email = c.String(),
						IsAdmin = c.Boolean(nullable: false),
						IsBanned = c.Boolean(nullable: false),
					})
				.PrimaryKey(t => t.Key);

			CreateTable(
				"MessangerGroups",
				c => new
					{
						Key = c.Int(nullable: false, identity: true),
						LastNudged = c.DateTime(),
						Name = c.String(maxLength: 200),
						Closed = c.Boolean(nullable: false),
						Topic = c.String(maxLength: 80),
						Welcome = c.String(maxLength: 200),
						Private = c.Boolean(nullable: false),
						InviteCode = c.String(maxLength: 6, fixedLength: true),
						Creator_Key = c.Int(),
					})
				.PrimaryKey(t => t.Key)
				.ForeignKey("ChatUsers", t => t.Creator_Key)
				.Index(t => t.Creator_Key);

			CreateTable(
				"Messages",
				c => new
					{
						Key = c.Int(nullable: false, identity: true),
						Id = c.String(),
						Subject = c.String(),
						From = c.String(),
						CC = c.String(),
						Content = c.String(),
						When = c.DateTimeOffset(nullable: false),
						Room_Key = c.Int(),
						User_Key = c.Int(),
					})
				.PrimaryKey(t => t.Key)
				.ForeignKey("MessangerGroups", t => t.Room_Key)
				.ForeignKey("ChatUsers", t => t.User_Key)
				.Index(t => t.Room_Key)
				.Index(t => t.User_Key);

			CreateTable(
				"GroupMessageUser1",
				c => new
					{
						ChatRoom_Key = c.Int(nullable: false),
						ChatUser_Key = c.Int(nullable: false),
					})
				.PrimaryKey(t => new { t.ChatRoom_Key, t.ChatUser_Key })
				.ForeignKey("MessangerGroups", t => t.ChatRoom_Key, cascadeDelete: true)
				.ForeignKey("ChatUsers", t => t.ChatUser_Key, cascadeDelete: true)
				.Index(t => t.ChatRoom_Key)
				.Index(t => t.ChatUser_Key);

			CreateTable(
				"GroupMessangerUsers",
				c => new
					{
						ChatRoom_Key = c.Int(nullable: false),
						ChatUser_Key = c.Int(nullable: false),
					})
				.PrimaryKey(t => new { t.ChatRoom_Key, t.ChatUser_Key })
				.ForeignKey("MessangerGroups", t => t.ChatRoom_Key, cascadeDelete: true)
				.ForeignKey("ChatUsers", t => t.ChatUser_Key, cascadeDelete: true)
				.Index(t => t.ChatRoom_Key)
				.Index(t => t.ChatUser_Key);

			CreateTable(
				"MessangerUserGroups",
				c => new
					{
						ChatRoom_Key = c.Int(nullable: false),
						ChatUser_Key = c.Int(nullable: false),
					})
				.PrimaryKey(t => new { t.ChatRoom_Key, t.ChatUser_Key })
				.ForeignKey("MessangerGroups", t => t.ChatRoom_Key, cascadeDelete: true)
				.ForeignKey("ChatUsers", t => t.ChatUser_Key, cascadeDelete: true)
				.Index(t => t.ChatRoom_Key)
				.Index(t => t.ChatUser_Key);

		}

		public override void Down()
		{
			DropIndex("MessangerUserGroups", new[] { "ChatUser_Key" });
			DropIndex("MessangerUserGroups", new[] { "ChatRoom_Key" });
			DropIndex("GroupMessangerUsers", new[] { "ChatUser_Key" });
			DropIndex("GroupMessangerUsers", new[] { "ChatRoom_Key" });
			DropIndex("GroupMessageUser1", new[] { "ChatUser_Key" });
			DropIndex("GroupMessageUser1", new[] { "ChatRoom_Key" });
			DropIndex("Messages", new[] { "User_Key" });
			DropIndex("Messages", new[] { "Room_Key" });
			DropIndex("MessangerGroups", new[] { "Creator_Key" });
			DropIndex("MessangerClients", new[] { "User_Key" });
			DropForeignKey("MessangerUserGroups", "ChatUser_Key", "ChatUsers");
			DropForeignKey("MessangerUserGroups", "ChatRoom_Key", "MessangerGroups");
			DropForeignKey("GroupMessangerUsers", "ChatUser_Key", "ChatUsers");
			DropForeignKey("GroupMessangerUsers", "ChatRoom_Key", "MessangerGroups");
			DropForeignKey("GroupMessageUser1", "ChatUser_Key", "ChatUsers");
			DropForeignKey("GroupMessageUser1", "ChatRoom_Key", "MessangerGroups");
			DropForeignKey("Messages", "User_Key", "ChatUsers");
			DropForeignKey("Messages", "Room_Key", "MessangerGroups");
			DropForeignKey("MessangerGroups", "Creator_Key", "ChatUsers");
			DropForeignKey("MessangerClients", "User_Key", "ChatUsers");
			DropTable("MessangerUserGroups");
			DropTable("GroupMessangerUsers");
			DropTable("GroupMessageUser1");
			DropTable("Messages");
			DropTable("MessangerGroups");
			DropTable("ChatUsers");
			DropTable("MessangerClients");
		}
	}
}

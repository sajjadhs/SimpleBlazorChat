using BlazorChatSample.Server.Data;
using BlazorChatSample.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorChatSample.Server.Hubs
{
    /// <summary>
    /// The SignalR hub 
    /// </summary>
    public class ChatHub : Hub
    {
        public ChatHub(DbCon dbCon)
        {
            DbCon = dbCon;

            DbCon.Database.EnsureCreated();
        }
        /// <summary>
        /// connectionId-to-username lookup
        /// </summary>
        /// <remarks>
        /// Needs to be static as the chat is created dynamically a lot
        /// </remarks>
        private static readonly Dictionary<string, string> userLookup = new Dictionary<string, string>();

        public DbCon DbCon { get; }

        /// <summary>
        /// Send a message to all clients
        /// </summary>
        /// <param name="username"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendMessage(string username, string message)
        {
            var user = await DbCon.Users.FirstOrDefaultAsync(q => q.Name == username);
            await DbCon.Messages.AddAsync(new Message { DateTime = DateTime.UtcNow, Text = message, From = user.Id });
            await DbCon.SaveChangesAsync();

            await Clients.All.SendAsync(Messages.RECEIVE, username, message);
        }

        public async Task TypingPing(string username)
        {
            var currentId = Context.ConnectionId;
            await Clients.AllExcept(currentId).SendAsync(Messages.TYPING_RECEIVE, username);
        }

        /// <summary>
        /// Register username
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public async Task Register(string username, string password)
        {
            var userr = DbCon.Users.FirstOrDefault(q => q.Name == username && q.Password == password);
            if (userr == null)
            {
                if (Startup.EnableMembering)
                {
                    userr = new User { Name = username, Password = password };
                    await DbCon.Users.AddAsync(userr);
                    await DbCon.SaveChangesAsync();
                }
                else
                {
                    return;
                }
            }

            var currentId = Context.ConnectionId;
            if (!userLookup.ContainsKey(currentId))
            {
                // maintain a lookup of connectionId-to-username
                userLookup.Add(currentId, username);
                // re-use existing message for now
                await Clients.AllExcept(currentId).SendAsync(
                    Messages.RECEIVE,
                    username, $"{username} joined the chat");

                await loadAll();
            }
        }

        private async Task loadAll()
        {

            var w = await DbCon.Messages.ToListAsync();
            var users = await DbCon.Users.ToListAsync();
            foreach (var item in w)
            {
                var from = users.FirstOrDefault(q => q.Id == item.From);
                await Clients.Caller.SendAsync(Messages.RECEIVE, from.Name + " " + item.DateTime.ToLocalTime(), item.Text);
            }
        }

        /// <summary>
        /// Log connection
        /// </summary>
        /// <returns></returns>
        public override Task OnConnectedAsync()
        {
            Console.WriteLine("Connected");
            return base.OnConnectedAsync();
        }

        /// <summary>
        /// Log disconnection
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception e)
        {
            Console.WriteLine($"Disconnected {e?.Message} {Context.ConnectionId}");
            // try to get connection
            string id = Context.ConnectionId;
            if (!userLookup.TryGetValue(id, out string username))
                username = "[unknown]";

            userLookup.Remove(id);
            await Clients.AllExcept(Context.ConnectionId).SendAsync(
                Messages.RECEIVE,
                username, $"{username} has left the chat");
            await base.OnDisconnectedAsync(e);
        }


    }
}

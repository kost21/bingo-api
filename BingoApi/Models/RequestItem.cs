using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BingoApi.Models
{
    public class RequestItem
    {
        public RequestItem()
        {

        }

        public Guid Id { get; set; } = new Guid();
    }
}
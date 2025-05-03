using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlugsportWienOgn.Database.Entities;

public class CleanupStamp
{
    public int Id { get; set; }
    public DateOnly LastCleanup { get; set; }
}

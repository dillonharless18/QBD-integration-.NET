using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace erpQB
{
    /**
     * This class represents objects that exist as their own entities.
     * 
     * IngressMessage extends this class to ensure there is a erpId coming in,
     * although there are other nested types that we must add to EgressMessages, 
     * and these also need a erpId. One example is an EgressMessage containing
     * items. These LineItems should therefore also extend this class.
     */
    public class ErpObject
    {
        public string erpId { get; set; }
    }
}

using System;

namespace EPS.Concurrency.Redis
{
	/// <summary>	A simple class that keeps the various queue names together.  </summary>
	/// <remarks>	6/28/2011. </remarks>
	public class QueueNames
	{
		private static QueueNames _default = new QueueNames("request", "pending", "poison");
		/// <summary>	Gets the name of the request queue. </summary>
		/// <value>	The request queue name. </value>
		public string Request { get; private set; }

		/// <summary>	Gets the name of the pending queue. </summary>
		/// <value>	The pending queue name. </value>
		public string Pending { get; private set; }

		/// <summary>	Gets the name of the poison queue. </summary>
		/// <value>	The poison queue name. </value>
		public string Poison { get; private set; }
		
		/// <summary>
		/// Initializes a new instance of the QueueNames class.
		/// </summary>
		public QueueNames(string request, string pending, string poison)
		{
			if (null == request) { throw new ArgumentNullException("request"); }
			if (string.IsNullOrWhiteSpace(request)) { throw new ArgumentException("must not be whitespace", "request"); }

			if (null == pending) { throw new ArgumentNullException("pending"); }
			if (string.IsNullOrWhiteSpace(pending)) { throw new ArgumentException("must not be whitespace", "pending"); }
	
			if (null == poison) { throw new ArgumentNullException("poison"); }
			if (string.IsNullOrWhiteSpace(poison)) { throw new ArgumentException("must not be whitespace", "poison"); }

			Request = request;
			Poison = poison;
			Pending = pending;
		}

		/// <summary>	Gets the default queue names of 'request', 'pending' and 'poison'. </summary>
		/// <value>	The default. </value>
		public static QueueNames Default 
		{
			get { return _default; }
		}
	}
}
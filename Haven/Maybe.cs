using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HavenUI;

public struct Maybe<T>
{
	public T Result;
	public bool Succeeded;

	public Maybe(T Result, bool Succeeded)
	{
		this.Result = Result;
		this.Succeeded = Succeeded;
	}

	public static Maybe<T> Success(T Result) => new(Result, true);
	public static Maybe<T> Fail() => new(default, false);

	public static implicit operator T(Maybe<T> other) => other.Result;
	public static implicit operator bool(Maybe<T> other) => other.Succeeded;
}

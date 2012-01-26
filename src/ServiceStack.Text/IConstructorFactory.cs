using System;

namespace ServiceStack.Text
{
    public interface IConstructorFactory
    {
        EmptyCtorDelegate Get(Type type);
    }
}

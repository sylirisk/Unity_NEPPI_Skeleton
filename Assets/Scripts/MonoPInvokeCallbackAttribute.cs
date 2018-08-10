using System;

public class MonoPInvokeCallbackAttribute : System.Attribute
{
     private Type type;
     public MonoPInvokeCallbackAttribute( Type t ) { type = t; }
}


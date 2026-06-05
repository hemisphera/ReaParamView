using System.Threading.Channels;
using Hsp.Osc;

namespace Hemisphera.Hulp.Plugin.Infrastructure;

public class OscProperty<T>
{
  private readonly ChannelWriter<IMessage> _channel;
  private T? _value;

  public string Address { get; }

  public T? Value
  {
    get => _value;
    set
    {
      var xValue = _value;
      _value = value;
      if (Equals(xValue, _value)) return;
      var msg = BuildMessage();
      if (msg == null) return;
      _channel.TryWrite(msg);
    }
  }

  private IMessage? BuildMessage()
  {
    return Value switch
    {
      int intVale => new Message(Address).PushAtom(intVale),
      double dblVal => new Message(Address).PushAtom(dblVal),
      float floatVal => new Message(Address).PushAtom(floatVal),
      string strVal => new Message(Address).PushAtom(strVal),
      bool boolVal => new Message(Address).PushAtom([new Atom(boolVal ? TypeTag.OscTrue : TypeTag.OscFalse)]),
      _ => null
    };
  }


  public OscProperty(ChannelWriter<IMessage> target, string address, T? value = default)
  {
    Address = address;
    _channel = target;
    _value = value;
  }
}
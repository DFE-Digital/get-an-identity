namespace TeacherIdentity.AuthServer.Tests;

public class FormUrlEncodedContentBuilder
{
    private readonly List<KeyValuePair<string, string?>> _values;

    public FormUrlEncodedContentBuilder()
    {
        _values = new List<KeyValuePair<string, string?>>();
    }

    public FormUrlEncodedContentBuilder Add(string key, object value)
    {
        if (value is not null)
        {
            _values.Add(new KeyValuePair<string, string?>(key, value.ToString()));
        }

        return this;
    }

    public FormUrlEncodedContentBuilder Add(string key, string value) =>
        Add(key, (object)value);

    public FormUrlEncodedContentBuilder Add<T>(string key, IEnumerable<T> values)
    {
        if (values is not null)
        {
            foreach (var value in values)
            {
                if (value is not null)
                {
                    _values.Add(new KeyValuePair<string, string?>(key, value.ToString()));
                }
            }
        }

        return this;
    }

    public FormUrlEncodedContent ToContent() => new(_values);
}

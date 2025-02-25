// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <summary>
/// <para>
/// Generates a new file at <see cref="OutputPath"/>.
/// </para>
/// <para>
/// The <see cref="TemplateFile"/> can define variables for substitution using <see cref="Properties"/>.
/// </para>
/// <example>
/// The input file might look like this:
/// <code>
/// 2 + 2 = ${Sum}
/// </code>
/// When the task is invoked like this, it will produce "2 + 2 = 4"
/// <code>
/// &lt;GenerateFileFromTemplate Properties="Sum=4;OtherValue=123;" ... &gt;
/// </code>
/// </example>
/// </summary>
public override bool Execute()
{
    ResolvedOutputPath = Path.GetFullPath(OutputPath.Replace('\\', '/'));

    if (!File.Exists(TemplateFile))
    {
        Log.LogError($"File {TemplateFile} does not exist");
        return false;
    }

    IDictionary<string, string> values = GetNamedProperties(Properties, Log);
    string template = File.ReadAllText(TemplateFile);

    string result = Replace(template, values);
    Directory.CreateDirectory(Path.GetDirectoryName(ResolvedOutputPath));
    File.WriteAllText(ResolvedOutputPath, result);

    return !Log.HasLoggedErrors;
}

public string Replace(string template, IDictionary<string, string> values)
{
    StringBuilder sb = new();
    StringBuilder varNameSb = new();
    int line = 1;
    for (int i = 0; i < template.Length; i++)
    {
        char templateChar = template[i];
        char nextTemplateChar = i + 1 >= template.Length
                        ? '\0'
                        : template[i + 1];

        // count lines in the template file
        if (templateChar == '\n')
        {
            line++;
        }

        if (templateChar == '`' && (nextTemplateChar == '$' || nextTemplateChar == '`'))
        {
            // skip the backtick for known escape characters
            i++;
            sb.Append(nextTemplateChar);
            continue;
        }

        if (templateChar != '$' || nextTemplateChar != '{')
        {
            // variables begin with ${. Moving on.
            sb.Append(templateChar);
            continue;
        }

        varNameSb.Clear();
        i += 2;
        for (; i < template.Length; i++)
        {
            templateChar = template[i];
            if (templateChar != '}')
            {
                varNameSb.Append(templateChar);
            }
            else
            {
                // Found the end of the variable substitution
                string varName = varNameSb.ToString();
                if (values.TryGetValue(varName, out string value))
                {
                    sb.Append(value);
                }
                else
                {
                    Log.LogWarning(null, null, null, TemplateFile,
                            line, 0, 0, 0,
                            message: $"No property value is available for '{varName}'");
                }

                varNameSb.Clear();
                break;
            }
        }

        if (varNameSb.Length > 0)
        {
            Log.LogWarning(null, null, null, TemplateFile,
                                    line, 0, 0, 0,
                                    message: "Expected closing bracket for variable placeholder. No substitution will be made.");
            sb.Append("${").Append(varNameSb.ToString());
        }
    }

    return sb.ToString();
}

public IDictionary<string, string> GetNamedProperties(string[] input, TaskLoggingHelper log)
{
    Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
    if (input == null)
    {
        return values;
    }

    foreach (string item in input)
    {
        int splitIdx = item.IndexOf('=');
        if (splitIdx < 0)
        {
            log.LogWarning($"Property: {item} does not have a valid '=' separator");
            continue;
        }

        string key = item.Substring(0, splitIdx).Trim();
        if (string.IsNullOrEmpty(key))
        {
            log.LogWarning($"Property: {item} does not have a valid property name");
            continue;
        }

        string value = item.Substring(splitIdx + 1);
        values[key] = value;
    }

    return values;
}
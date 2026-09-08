namespace Ciir.Application.Ports;

/// <summary>Creates the <see cref="ICiirWriter"/> for a given output directory.</summary>
public interface ICiirWriterFactory
{
    /// <summary>Creates a writer that (re)creates the CIIR output file under <paramref name="outputDirectory"/>.</summary>
    /// <param name="outputDirectory">The directory CIIR output artifacts are written to.</param>
    ICiirWriter Create(string outputDirectory);
}

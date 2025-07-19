public interface ISavable
{
    // O ID �nico do objeto. A propriedade 'get' significa que s� podemos ler o valor.
    string ID { get; }

    // O contrato para capturar e restaurar o estado continua o mesmo.
    object CaptureState();
    void RestoreState(object state);
}
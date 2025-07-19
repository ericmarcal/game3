public interface ISavable
{
    // O ID único do objeto. A propriedade 'get' significa que só podemos ler o valor.
    string ID { get; }

    // O contrato para capturar e restaurar o estado continua o mesmo.
    object CaptureState();
    void RestoreState(object state);
}
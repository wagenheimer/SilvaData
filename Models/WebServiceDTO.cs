namespace SilvaData.Models
{
    // --- DTOs de Requisição ---

    public class dataLoginParameters
    {
        public string? dispositivoDescricao { get; set; }
        public string? dispositivoId { get; set; }
        public string? email { get; set; }
        public string? senha { get; set; }
    }

    public class getDataDefaultParameters
    {
        public string? data { get; set; }
        public string? dispositivoId { get; set; }
        public string? idioma { get; set; }
        public string? session { get; set; }
        public string? usuario { get; set; }
    }

    public class getLotesParameters : getDataDefaultParameters
    {
        public int status { get; set; }

        public getLotesParameters() { }

        public getLotesParameters(getDataDefaultParameters baseParams)
        {
            usuario = baseParams.usuario;
            dispositivoId = baseParams.dispositivoId;
            session = baseParams.session;
            idioma = baseParams.idioma;
            data = baseParams.data;
        }
    }

    public class getLoteFormsParameters : getDataDefaultParameters
    {
        public int? lote { get; set; }
        public string? loteFormFase { get; set; }

        public getLoteFormsParameters() { }

        public getLoteFormsParameters(getDataDefaultParameters baseParams)
        {
            usuario = baseParams.usuario;
            dispositivoId = baseParams.dispositivoId;
            session = baseParams.session;
            idioma = baseParams.idioma;
            data = baseParams.data;
        }
    }

    public class checkSessionAtivaParameters
    {
        public string? email { get; set; }
        public string? senha { get; set; }
    }

    public class checkUserSessionAtivaParameters
    {
        public string? dispositivoId { get; set; }
        public string? session { get; set; }
        public string? usuario { get; set; }
    }

    public class postTermosParameters
    {
        public string? dispositivoId { get; set; }
        public string? session { get; set; }
        public string? usuario { get; set; }
        public string? dispositivoDescricao { get; set; }
        public string? dataAceite { get; set; }
    }

    public class postNpsParameters
    {
        public string? usuario { get; set; }
        public string? nps { get; set; }
        public string? dispositivoId { get; set; }
        public string? session { get; set; }
        public string? npsData { get; set; }
    }

    // --- DTOs de Resposta ---

    public class dataLoginResult
    {
        public string? id { get; set; }
        public string? nome { get; set; }
        public string? email { get; set; }
        public string? dispositivoIdAceite { get; set; }
        public string? dispositivoDescricaoAceite { get; set; }
        public string? dataAceiteApp { get; set; }
        public string? ipAceiteApp { get; set; }
        public string? session { get; set; }
        public string? urlFoto { get; set; }
        public string? nps { get; set; }
        public string? npsData { get; set; }
        public bool npsPrecisaEnviar { get; set; } = false;
        public string? urlImagem { get; set; }
        public string dispositivoId => Preferences.Get("my_id", string.Empty);
    }

    public class checkSessionAtivaResult
    {
        public string? dataFim { get; set; }
        public string? dataInicio { get; set; }
        public string? dispositivoDescricao { get; set; }
        public string? dispositivoId { get; set; }
        public string? id { get; set; }
        public int? status { get; set; }
    }

    public class ISIWebServiceResult
    {
        public string? data { get; set; }
        public string? mensagem { get; set; }
        public bool sucesso { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public bool RetryRequested { get; set; }
    }
}

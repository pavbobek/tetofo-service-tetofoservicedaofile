namespace tetofo.Service.DAO.Exception;

public class DAOException : System.Exception {
    public DAOException(string m) : base(m) {}
    public DAOException(string m, System.Exception e) : base(m, e) {}
}
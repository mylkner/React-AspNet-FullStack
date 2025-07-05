import { Routes, Route } from "react-router";
import Login from "./pages/Login";
import Register from "./pages/Register";
import AuthLayout from "./routes/AuthLayout";
import AdminPage from "./pages/AdminPage";
import NonAdminPage from "./pages/NonAdminPage";
import AdminLayout from "./routes/AdminLayout";

function App() {
    return (
        <Routes>
            <Route path="/" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route element={<AuthLayout />}>
                <Route path="/UserHome.tsx">
                    <Route path="NonAdminPage.tsx" element={<NonAdminPage />} />
                    <Route element={<AdminLayout />}>
                        <Route path="AdminPage.tsx" element={<AdminPage />} />
                    </Route>
                </Route>
            </Route>
        </Routes>
    );
}

export default App;
